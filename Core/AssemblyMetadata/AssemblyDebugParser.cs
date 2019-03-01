using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection.Metadata;
using System.Text;
using Microsoft.DiaSymReader.Tools;
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

        private static readonly Guid SourceLinkGuid = new Guid("CC110556-A091-4D38-9FEC-25AB9A351A6A");


        public AssemblyDebugData GetDebugData()
        {
            var debugData = new AssemblyDebugData
            {
                PdbType = _pdbType,
                Sources = GetSourceDocuments(),
                SourceLink = GetSourceLinkInformation()
            };

            return debugData;
        }

        private IReadOnlyList<SourceLinkMap> GetSourceLinkInformation()
        {
            var sl = (from cdi in _reader.CustomDebugInformation
                      let cd = _reader.GetCustomDebugInformation(cdi)
                      let kind = _reader.GetGuid(cd.Kind)
                      where kind == SourceLinkGuid
                      let bl = _reader.GetBlobBytes(cd.Value)
                      select Encoding.UTF8.GetString(bl))
                .FirstOrDefault();

            if (sl != null)
            {
                try
                {
                    /* Some tools, like GitLink generate incorrectly escaped JSON:

                    {
                    "documents": {
                        "C:\projects\cefsharp\*": "https://raw.github.com/CefSharp/CefSharp/4f88ad11416e93dde4b52216800efd42ba3b9c8a/*"
                      }
                    }

                    Catch the exception and try to fix the key
                    */
                    JObject? jobj = null;
                    try
                    {
                        jobj = JObject.Parse(sl);
                    }
                    catch (JsonReaderException e) when (e.Path == "documents")
                    {
                        sl = sl.Replace(@"\", @"\\");
                        jobj = JObject.Parse(sl);
                    }

                    var docs = (JObject)jobj["documents"];

                    var slis = (from prop in docs.Properties()
                                select new SourceLinkMap
                                {
                                    Base = prop.Name.Replace(@"\", @"/"), // use forward slashes for the url,
                                    Location = prop.Value.Value<string>()
                                })
                        .ToList();

                    return slis;
                }
                catch (JsonReaderException jse)
                {
                    throw new InvalidDataException("SourceLink data could not be parsed", jse);
                }

            }

            return Array.Empty<SourceLinkMap>();
        }


        private IReadOnlyList<AssemblyDebugSourceDocument> GetSourceDocuments()
        {
            var docs = (from docHandle in _reader.Documents
                        let document = _reader.GetDocument(docHandle)
                        select new AssemblyDebugSourceDocument
                        (
                            _reader.GetString(document.Name).Replace(@"\", @"/"), // use forward slashes for the url
                            _reader.GetBlobBytes(document.Hash),
                            _reader.GetGuid(document.Language),
                            _reader.GetGuid(document.HashAlgorithm)
                        )).ToList();

            return docs;
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
