using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using NuGetPe.AssemblyMetadata;

namespace PackageExplorerViewModel
{
    public class AssemblyDebugSourceDocumentViewModel
    {
        private readonly AssemblyDebugSourceDocument _sourceDocument;

        public AssemblyDebugSourceDocumentViewModel(AssemblyDebugSourceDocument sourceDocument, string path, string? location)
        {
            _sourceDocument = sourceDocument ?? throw new ArgumentNullException(nameof(sourceDocument));
            Path = path;
            Location = location;
        }

        public string Path { get; }
        public string? Location { get; }

        public string HashAlgorithm => _sourceDocument.HashAlgorithm.Name;

        public SymbolLanguage Language => _sourceDocument.Language;
    }
}
