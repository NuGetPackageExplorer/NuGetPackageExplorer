using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using NuGetPe.AssemblyMetadata;

namespace PackageExplorerViewModel
{
    public class AssemblyDebugDataViewModel : ViewModelBase
    {
        private readonly Task<AssemblyDebugData> _debugData;

        public AssemblyDebugDataViewModel(Task<AssemblyDebugData> debugData)
        {
            _debugData = debugData ?? throw new ArgumentNullException(nameof(debugData));
            WaitForData();
        }

        private async void WaitForData()
        {
            try
            {
                var debugData = await _debugData;
                if (debugData != null)
                {
                    Sources = CreateSourcesViewModels(debugData);
                    PdbType = debugData.PdbType;

                    MetadataReferences = debugData.MetadataReferences.OrderBy(r => r.Name).ToList();
                    CompilerFlags = debugData.CompilerFlags.OrderBy(f => f.Key).ToList();
                    HasCompilerFlags = debugData.HasCompilerFlags;
                }

                OnPropertyChanged(null); // refresh all properties
            }
            catch
            {

            }
            
        }

        public PdbType PdbType { get; private set; }

        public IReadOnlyList<AssemblyDebugSourceDocumentViewModel>? Sources { get; private set; }

        public IReadOnlyCollection<MetadataReference>? MetadataReferences { get; private set; }
        public IReadOnlyCollection<CompilerFlag>? CompilerFlags { get; private set; }


        public bool HasCompilerFlags { get; private set; }

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
