using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection.Metadata;
using System.Text;
using Microsoft.DiaSymReader.Tools;
using Microsoft.SourceLink.Tools;

using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

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

        }

        public AssemblyDebugParser(MetadataReaderProvider readerProvider, PdbType pdbType)
        {
            _readerProvider = readerProvider;
            _pdbType = pdbType;

            // Possible BadImageFormatException if a full PDB is passed
            // in. We'll let the throw bubble up to something that can handle it
            _reader = _readerProvider.GetMetadataReader();
        }

        private readonly PdbType _pdbType;
        private bool _disposedValue = false;
        private readonly MetadataReaderProvider _readerProvider;
        private readonly MetadataReader _reader;
        private readonly Stream? _temporaryPdbStream;

        private static readonly Guid SourceLinkId = new Guid("CC110556-A091-4D38-9FEC-25AB9A351A6A");

        private static readonly Guid EmbeddedSourceId = new Guid("0E8A571B-6926-466E-B4AD-8AB04611F5FE");


        public AssemblyDebugData GetDebugData()
        {

            var (documents, errors) = GetDocumentsWithUrls();
            var debugData = new AssemblyDebugData
            {
                PdbType = _pdbType,
                Sources = documents,
                SourceLinkErrors = errors
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
                _disposedValue = true;
            }
        }
    }
}
