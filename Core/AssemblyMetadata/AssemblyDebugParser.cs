using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection.Metadata;

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


        public IReadOnlyList<AssemblyDebugData> GetDebugData()
        {
            var docs = (from docHandle in _reader.Documents
                       let document = _reader.GetDocument(docHandle)
                       select new AssemblyDebugData
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
