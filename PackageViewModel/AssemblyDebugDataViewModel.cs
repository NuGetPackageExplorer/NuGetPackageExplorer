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

            var lookup = debugData.SourceLink.ToDictionary(sm => sm.Base[0..^1], sm => sm.Location[0..^1]);

            foreach (var doc in debugData.Sources)
            {
                var path = doc.Name;
                string? location = null;
                // see if any keys match the base
                foreach (var key in lookup.Keys)
                {
                    if (doc.Name.StartsWith(key, StringComparison.Ordinal))
                    {
                        path = doc.Name.Substring(key.Length);
                        location = lookup[key] + path;

                        break;
                    }
                }

                list.Add(new AssemblyDebugSourceDocumentViewModel(doc, path, location));
            }

            return list;
        }

    }
}
