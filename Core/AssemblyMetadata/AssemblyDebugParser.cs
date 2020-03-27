using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection.Metadata;
using System.Reflection.PortableExecutable;
using System.Text;
using Microsoft.DiaSymReader.Tools;
using Microsoft.SourceLink.Tools;

namespace NuGetPe.AssemblyMetadata
{
    internal sealed class AssemblyDebugParser : IDisposable
    {
        public AssemblyDebugParser(Stream? peStream, Stream pdbStream)
        {
            Stream inputStream;
            if (!PdbConverter.IsPortable(pdbStream))
            {
                if (peStream == null)
                    throw new ArgumentNullException(nameof(peStream), "Full PDB's require the PE file to be next to the PDB");
                // Full PDB. convert to ppdb in memory
                _temporaryPdbStream = new MemoryStream();
                PdbConverter.Default.ConvertWindowsToPortable(peStream, pdbStream, _temporaryPdbStream);
                _temporaryPdbStream.Position = 0;
                peStream.Position = 0;
                inputStream = _temporaryPdbStream;
                _pdbType = PdbType.Full;
            }
            else
            {
                inputStream = pdbStream;
                _pdbType = PdbType.Portable;
            }

            _readerProvider = MetadataReaderProvider.FromPortablePdbStream(inputStream);
            _reader = _readerProvider.GetMetadataReader();
            _peReader = new PEReader(peStream!);
            _ownPeReader = true;
        }

        public AssemblyDebugParser(PEReader peReader, MetadataReaderProvider readerProvider, PdbType pdbType)
        {
            _readerProvider = readerProvider;
            _pdbType = pdbType;
            _peReader = peReader;
            _ownPeReader = false;

            // Possible BadImageFormatException if a full PDB is passed
            // in. We'll let the throw bubble up to something that can handle it
            _reader = _readerProvider.GetMetadataReader();
        }

        private readonly PdbType _pdbType;
        private bool _disposedValue = false;
        private readonly MetadataReaderProvider _readerProvider;
        private readonly MetadataReader _reader;
        private readonly PEReader _peReader;
        private readonly bool _ownPeReader;
        private readonly Stream? _temporaryPdbStream;

        private static readonly Guid SourceLinkId = new Guid("CC110556-A091-4D38-9FEC-25AB9A351A6A");

        private static readonly Guid EmbeddedSourceId = new Guid("0E8A571B-6926-466E-B4AD-8AB04611F5FE");
        private const ushort PortableCodeViewVersionMagic = 0x504d;


        public AssemblyDebugData GetDebugData()
        {

            var (documents, errors) = GetDocumentsWithUrls();


            var debugData = new AssemblyDebugData
            {
                PdbType = _pdbType,
                Sources = documents,
                SourceLinkErrors = errors,
                SymbolKeys = GetSymbolKeys(_peReader),
                HasDebugInfo = true
            };

            return debugData;
        }


        private byte[]? GetSourceLinkBytes()
        {
            if (_reader == null) return null;
            var blobh = default(BlobHandle);
            foreach (var cdih in _reader.GetCustomDebugInformation(EntityHandle.ModuleDefinition))
            {
                var cdi = _reader.GetCustomDebugInformation(cdih);
                if (_reader.GetGuid(cdi.Kind) == SourceLinkId)
                    blobh = cdi.Value;
            }
            if (blobh.IsNil) return Array.Empty<byte>();
            return _reader.GetBlobBytes(blobh);
        }

        private bool IsEmbedded(DocumentHandle dh)
        {
            foreach (var cdih in _reader.GetCustomDebugInformation(dh))
            {
                var cdi = _reader.GetCustomDebugInformation(cdih);
                if (_reader.GetGuid(cdi.Kind) == EmbeddedSourceId)
                    return true;
            }
            return false;
        }

        public static IReadOnlyList<SymbolKey> GetSymbolKeys(PEReader peReader)
        {
            var result = new List<SymbolKey>();
            var checksums = new List<string>();

            foreach (var entry in peReader.ReadDebugDirectory())
            {
                if (entry.Type != DebugDirectoryEntryType.PdbChecksum) continue;

                var data = peReader.ReadPdbChecksumDebugDirectoryData(entry);
                var algorithm = data.AlgorithmName;
                var checksum = data.Checksum.Select(b => b.ToString("x2", CultureInfo.InvariantCulture));

                checksums.Add($"{algorithm}:{checksum}");
            }

            foreach (var entry in peReader.ReadDebugDirectory())
            {
                if (entry.Type != DebugDirectoryEntryType.CodeView) continue;

                var data = peReader.ReadCodeViewDebugDirectoryData(entry);
                var isPortable = entry.MinorVersion == PortableCodeViewVersionMagic;

                var signature = data.Guid;
                var age = data.Age;
#pragma warning disable CA1308 // Normalize strings to uppercase
                var file = Uri.EscapeDataString(Path.GetFileName(data.Path.Replace("\\", "/", StringComparison.OrdinalIgnoreCase)).ToLowerInvariant());
#pragma warning restore CA1308 // Normalize strings to uppercase

                // Portable PDBs, see: https://github.com/dotnet/symstore/blob/83032682c049a2b879790c615c27fbc785b254eb/src/Microsoft.SymbolStore/KeyGenerators/PortablePDBFileKeyGenerator.cs#L84
                // Windows PDBs, see: https://github.com/dotnet/symstore/blob/83032682c049a2b879790c615c27fbc785b254eb/src/Microsoft.SymbolStore/KeyGenerators/PDBFileKeyGenerator.cs#L52
                var symbolId = isPortable
                    ? signature.ToString("N", CultureInfo.InvariantCulture) + "FFFFFFFF"
                    : string.Format(CultureInfo.InvariantCulture, "{0}{1:x}", signature.ToString("N", CultureInfo.InvariantCulture), age);

                result.Add(new SymbolKey
                {
                    IsPortablePdb = isPortable,
                    Checksums = checksums,
                    Key = $"{file}/{symbolId}/{file}",
                });
            }

            return result;
        }


        private IEnumerable<AssemblyDebugSourceDocument> GetSourceDocuments()
        {
            foreach (var dh in _reader.Documents)
            {
                if (dh.IsNil) continue;
                var d = _reader.GetDocument(dh);
                if (d.Name.IsNil || d.Language.IsNil || d.HashAlgorithm.IsNil || d.Hash.IsNil) continue;

                var name = _reader.GetString(d.Name);
                var language = _reader.GetGuid(d.Language);
                var hashAlgorithm = _reader.GetGuid(d.HashAlgorithm);
                var hash = _reader.GetBlobBytes(d.Hash);
                var isEmbedded = IsEmbedded(dh);


                var doc = new AssemblyDebugSourceDocument
                (
                    name,
                    hash,
                    language,
                    hashAlgorithm,
                    isEmbedded
                );
               
                if (doc.Language == SymbolLanguage.Unknown)
                {
                    DiagnosticsClient.TrackEvent("Unknown language Guid", new Dictionary<string, string>
                    {
                        { "LanguageGuid", language.ToString() },
                        { "HashGuid", hashAlgorithm.ToString() },
                        { "DocExtension", Path.GetExtension(name)! }
                    });
                }

                yield return doc;
            }
        }


        private (IReadOnlyList<AssemblyDebugSourceDocument> documents, IReadOnlyList<string> errors) GetDocumentsWithUrls()
        {
            var bytes = GetSourceLinkBytes();
            SourceLinkMap? map = null;

            var errors = new List<string>();
            if (bytes != null && bytes.Length > 0)
            {
                var text = Encoding.UTF8.GetString(bytes);
                map = SourceLinkMap.Parse(text, errors.Add);
            }

            var list = new List<AssemblyDebugSourceDocument>();

            foreach (var doc in GetSourceDocuments())
            {
                if (!doc.IsEmbedded)
                    doc.Url = map?.GetUri(doc.Name);
                list.Add(doc);
            }

            return (list, errors);
        }




        public void Dispose()
        {
            if (!_disposedValue)
            {
                _readerProvider.Dispose();
                _temporaryPdbStream?.Dispose();

                if(_ownPeReader)
                    _peReader.Dispose();

                _disposedValue = true;
            }
        }
    }

    [DebuggerDisplay("{Key}, IsPortable={IsPortablePdb}")]
    public class SymbolKey
    {
        public bool IsPortablePdb { get; set; }
#pragma warning disable CS8618 // Non-nullable field is uninitialized. Consider declaring as nullable.
        public IReadOnlyList<string> Checksums { get; set; }
        public string Key { get; set; }
#pragma warning restore CS8618 // Non-nullable field is uninitialized. Consider declaring as nullable.
    }
}
