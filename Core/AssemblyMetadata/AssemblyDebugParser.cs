using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection.Metadata;
using System.Reflection.PortableExecutable;
using System.Security.Cryptography;
using System.Text;
using Microsoft.DiaSymReader.Tools;
using Microsoft.FileFormats;
using Microsoft.FileFormats.PDB;
using Microsoft.FileFormats.PE;
using Microsoft.SourceLink.Tools;

namespace NuGetPe.AssemblyMetadata
{
    internal sealed class AssemblyDebugParser : IDisposable
    {
        public AssemblyDebugParser(Stream? peStream, Stream pdbStream)
        {
            Stream? inputStream;
            if (!PdbConverter.IsPortable(pdbStream))
            {
                if (peStream == null)
                    throw new ArgumentNullException(nameof(peStream), "Full PDB's require the PE file to be next to the PDB");

                if (!AppCompat.IsSupported(RuntimeFeature.DiaSymReader))
                    throw new PlatformNotSupportedException("Windows PDB cannot be processed on this platform.");

                // Full PDB. convert to ppdb in memory

                _pdbBytes = pdbStream.ReadAllBytes();
                pdbStream.Position = 0;
                _peBytes = peStream.ReadAllBytes();
                peStream.Position = 0;

                _temporaryPdbStream = new MemoryStream();

                try
                {
                    PdbConverter.Default.ConvertWindowsToPortable(peStream, pdbStream, _temporaryPdbStream);
                    _temporaryPdbStream.Position = 0;
                }
                catch (Exception)
                {
                    _temporaryPdbStream?.Dispose();
                    _temporaryPdbStream = null;
                }
                peStream.Position = 0;
                inputStream = _temporaryPdbStream;
                _pdbType = PdbType.Full;
            }
            else
            {
                inputStream = pdbStream;
                _pdbType = PdbType.Portable;
                _pdbBytes = pdbStream.ReadAllBytes();
                pdbStream.Position = 0;
            }


            if (inputStream != null)
            {
                _readerProvider = MetadataReaderProvider.FromPortablePdbStream(inputStream);
                _reader = _readerProvider.GetMetadataReader();
            }

            if (peStream != null)
            {
                _peReader = new PEReader(peStream);
                _ownPeReader = true;
            }
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
        private bool _disposedValue;
        private readonly MetadataReaderProvider? _readerProvider;
        private readonly MetadataReader? _reader;
        private readonly PEReader? _peReader;
        private readonly bool _ownPeReader;
        private readonly Stream? _temporaryPdbStream;
        private readonly byte[]? _pdbBytes;
        private readonly byte[]? _peBytes;

        private static readonly Guid SourceLinkId = new Guid("CC110556-A091-4D38-9FEC-25AB9A351A6A");
        private static readonly Guid EmbeddedSourceId = new Guid("0E8A571B-6926-466E-B4AD-8AB04611F5FE");
        private static readonly Guid CompilerFlagsId = new Guid("B5FEEC05-8CD0-4A83-96DA-466284BB4BD8");
        private static readonly Guid MetadataReferencesId = new Guid("7E4D4708-096E-4C5C-AEDA-CB10BA6A740D");


        private const ushort PortableCodeViewVersionMagic = 0x504d;


        public AssemblyDebugData GetDebugData()
        {

            var (documents, errors) = GetDocumentsWithUrls();


            var debugData = new AssemblyDebugData
            {
                PdbType = _pdbType,
                Sources = documents,
                SourceLinkErrors = errors,
                PdbChecksumIsValid = VerifyPdbChecksums(),
                CompilerFlags = GetCompilerFlags(),
                MetadataReferences = GetMetadataReferences(),
                HasDebugInfo = true
            };
            if (_peReader != null)
            {
                debugData.SymbolKeys = GetSymbolKeys(_peReader);
            }

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

        private IReadOnlyCollection<CompilerFlag> GetCompilerFlags()
        {
            var flags = new List<CompilerFlag>();

            if (_reader is null)
                return flags;

            foreach (var cdih in _reader.GetCustomDebugInformation(EntityHandle.ModuleDefinition))
            {
                var customDebugInformation = _reader.GetCustomDebugInformation(cdih);
                if (_reader.GetGuid(customDebugInformation.Kind) == CompilerFlagsId)
                {
                    var blobReader = _reader.GetBlobReader(customDebugInformation.Value);

                    // Compiler flag bytes are UTF-8 null-terminated key-value pairs
                    var nullIndex = blobReader.IndexOf(0);
                    while (nullIndex >= 0)
                    {
                        var key = blobReader.ReadUTF8(nullIndex);

                        // Skip the null terminator
                        blobReader.ReadByte();

                        nullIndex = blobReader.IndexOf(0);
                        var value = blobReader.ReadUTF8(nullIndex);

                        // Skip the null terminator
                        blobReader.ReadByte();

                        nullIndex = blobReader.IndexOf(0);

                        // key and value now have strings containing serialized compiler flag information
                        flags.Add(new CompilerFlag { Key = key, Value = value });
                    }
                }
            }

            return flags;
        }

        private IReadOnlyCollection<MetadataReference> GetMetadataReferences()
        {
            var references = new List<MetadataReference>();

            if (_reader is null)
                return references;

            foreach (var cdih in _reader.GetCustomDebugInformation(EntityHandle.ModuleDefinition))
            {
                var customDebugInformation = _reader.GetCustomDebugInformation(cdih);
                if (_reader.GetGuid(customDebugInformation.Kind) == MetadataReferencesId)
                {
                    var blobReader = _reader.GetBlobReader(customDebugInformation.Value);

                    // Each loop is one reference
                    while (blobReader.RemainingBytes > 0)
                    {
                        // Order of information
                        // File name (null terminated string): A.exe
                        // Extern Alias (null terminated string): a1,a2,a3
                        // EmbedInteropTypes/MetadataImageKind (byte)
                        // COFF header Timestamp field (4 byte int)
                        // COFF header SizeOfImage field (4 byte int)
                        // MVID (Guid, 24 bytes)

                        var terminatorIndex = blobReader.IndexOf(0);

                        var name = blobReader.ReadUTF8(terminatorIndex);

                        // Skip the null terminator
                        blobReader.ReadByte();

                        terminatorIndex = blobReader.IndexOf(0);

                        var externAliases = blobReader.ReadUTF8(terminatorIndex);

                        // Skip the null terminator
                        blobReader.ReadByte();

                        var embedInteropTypesAndKind = blobReader.ReadByte();
                        var embedInteropTypes = (embedInteropTypesAndKind & 0b10) == 0b10;
                        var kind = (embedInteropTypesAndKind & 0b1) == 0b1
                            ? MetadataImageKind.Assembly
                            : MetadataImageKind.Module;

                        var timestamp = blobReader.ReadInt32(); // 4B hash (part of SHA256) for deterministic builds
                        var imageSize = blobReader.ReadInt32();
                        var mvid = blobReader.ReadGuid();

                        references.Add(new MetadataReference
                        {
                            Name = name,
                            ExternAliases = string.IsNullOrEmpty(externAliases) ? ImmutableArray<string>.Empty : externAliases.Split(',').ToImmutableArray(),
                            EmbedInteropTypes = embedInteropTypes,
                            MetadataImageKind = kind,
                            Timestamp = timestamp,
                            ImageSize = imageSize,
                            Mvid = mvid
                        });

                    }
                }
            }
            return references;
        }



        private bool IsEmbedded(DocumentHandle dh)
        {
            if (_reader is null)
                return false;

            foreach (var cdih in _reader.GetCustomDebugInformation(dh))
            {
                var cdi = _reader.GetCustomDebugInformation(cdih);
                if (_reader.GetGuid(cdi.Kind) == EmbeddedSourceId)
                    return true;
            }
            return false;
        }

        // reference: https://github.com/NuGet/NuGet.Jobs/blob/26c23697fee363d3133171f71e129cda9b5a3707/src/Validation.Symbols/SymbolsValidatorService.cs#L186

        private bool VerifyPdbChecksums()
        {
            // Nothing to verify as the pdb is inside the PE file
            if (_pdbType == PdbType.Embedded)
                return true;

            if (_pdbType == PdbType.Portable)
            {
                if (_peReader == null)
                {
                    return false;
                }

                var checksumRecords = _peReader.ReadDebugDirectory()
                                               .Where(entry => entry.Type == DebugDirectoryEntryType.PdbChecksum)
                                               .Select(_peReader.ReadPdbChecksumDebugDirectoryData)
                                               .ToList();

                if (checksumRecords.Count == 0)
                {
                    return false;
                }

                var hashes = new Dictionary<string, byte[]>();

                if (_reader.DebugMetadataHeader == null)
                    return false;

                var idOffset = _reader.DebugMetadataHeader.IdStartOffset;

                foreach (var checksumRecord in checksumRecords)
                {
                    if (!hashes.TryGetValue(checksumRecord.AlgorithmName, out var hash))
                    {
                        var han = new HashAlgorithmName(checksumRecord.AlgorithmName);
                        using (var hashAlg = IncrementalHash.CreateHash(han))
                        {
                            hashAlg.AppendData(_pdbBytes!, 0, idOffset);
                            hashAlg.AppendData(new byte[20]);
                            var offset = idOffset + 20;
                            var count = _pdbBytes!.Length - offset;
                            hashAlg.AppendData(_pdbBytes!, offset, count);
                            hash = hashAlg.GetHashAndReset();
                        }
                        hashes.Add(checksumRecord.AlgorithmName, hash);
                    }
                    if (checksumRecord.Checksum.ToArray().SequenceEqual(hash))
                    {
                        // found the right checksum
                        return true;
                    }
                }

                // Not found any checksum record that matches the PDB.
                return false;
            }

            // Deal with Windows PDB's

            using var pdbBytesStream = new MemoryStream(_pdbBytes!);
            var pdbFile = new PDBFile(new StreamAddressSpace(pdbBytesStream));

            using var peBytesStream = new MemoryStream(_peBytes!);
            var peFile = new PEFile(new StreamAddressSpace(peBytesStream));

            var pdb = peFile.Pdbs.FirstOrDefault(p => p.Signature == pdbFile.Signature && p.Age == pdbFile.Age);

            if (pdb != null)
            {
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
                var checksum = string.Concat(data.Checksum.Select(b => b.ToString("x2", CultureInfo.InvariantCulture)));

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
            if (_reader is null)
                yield break;

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
            var list = new List<AssemblyDebugSourceDocument>();

            if (bytes != null && bytes.Length > 0)
            {
                var text = Encoding.UTF8.GetString(bytes);

                try
                {
                    map = SourceLinkMap.Parse(text);
                }
                catch(Exception e)
                {
                    errors.Add($"Source Link data is invalid. Error: '{e.Message}'");
                }
            }

            foreach (var doc in GetSourceDocuments())
            {
                if(doc.IsEmbedded)
                {
                    list.Add(doc);
                }
                else
                {
                    string? uri = default;
                    if (map?.TryGetUri(doc.Name, out uri) == true)
                    {
                        doc.Url = uri!;
                        list.Add(doc);
                    }
                    else
                    {
                        list.Add(doc);
                    }
                }
                    
            }

            return (list, errors);
        }

        public void Dispose()
        {
            if (!_disposedValue)
            {
                _readerProvider?.Dispose();
                _temporaryPdbStream?.Dispose();

                if (_ownPeReader)
                    _peReader?.Dispose();

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
