using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection.Metadata;
using System.Text;
using Newtonsoft.Json.Linq;

namespace NuGetPe.AssemblyMetadata
{
    internal sealed class AssemblyDebugParser : IDisposable
    {

        public AssemblyDebugParser(MetadataReaderProvider readerProvider)
        {
            _readerProvider = readerProvider;
            _reader = _readerProvider.GetMetadataReader();
        }

        private bool _disposedValue = false;
        private readonly MetadataReaderProvider _readerProvider;
        private readonly MetadataReader _reader;

        private static readonly Guid SourceLinkGuid = new Guid("CC110556-A091-4D38-9FEC-25AB9A351A6A");


        public AssemblyDebugData GetDebugData()
        {
            var debugData = new AssemblyDebugData();

            

            debugData.Sources = GetSourceDocuments();
            debugData.SourceLink = GetSourceLinkInformation(); 

            return debugData;
        }

        private IReadOnlyList<SourceLinkMap> GetSourceLinkInformation()
        {
            var sl = (from cdi in _reader.CustomDebugInformation
                       let cd = _reader.GetCustomDebugInformation(cdi)
                       let kind = _reader.GetGuid(cd.Kind)
                       where kind == SourceLinkGuid
                       let bl =  _reader.GetBlobBytes(cd.Value)
                       select Encoding.UTF8.GetString(bl))
                .FirstOrDefault();

            var jobj = JObject.Parse(sl);
            var docs = (JObject)jobj["documents"];


            var slis = (from prop in docs.Properties()
                       select new SourceLinkMap
                       {
                           Base = prop.Name,
                           Location = prop.Value.Value<string>()
                       })
                .ToList();

            return slis;
        }


        private IReadOnlyList<AssemblyDebugSourceDocument> GetSourceDocuments()
        {
            var docs = (from docHandle in _reader.Documents
                        let document = _reader.GetDocument(docHandle)
                        select new AssemblyDebugSourceDocument
                        (
                            _reader.GetString(document.Name),
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
                _disposedValue = true;
            }
        }
    }
}
