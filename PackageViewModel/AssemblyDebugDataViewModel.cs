using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using NuGetPe.AssemblyMetadata;

namespace PackageExplorerViewModel
{
    public class AssemblyDebugDataViewModel
    {
        private readonly AssemblyDebugData _debugData;

        public AssemblyDebugDataViewModel(AssemblyDebugData debugData)
        {
            _debugData = debugData ?? throw new ArgumentNullException(nameof(debugData));
            Sources = CreateSourcesViewModels(debugData);
        }

        public PdbType PdbType => _debugData.PdbType;

        public IReadOnlyList<AssemblyDebugSourceDocumentViewModel> Sources { get; }

        private static IReadOnlyList<AssemblyDebugSourceDocumentViewModel> CreateSourcesViewModels(AssemblyDebugData debugData)
        {
            var list = new List<AssemblyDebugSourceDocumentViewModel>(debugData.Sources.Count);

            foreach (var doc in debugData.Sources)
            {
                list.Add(new AssemblyDebugSourceDocumentViewModel(doc, doc.Name, doc.Url, doc.IsEmbedded));
            }

            return list;
        }

    }
}
