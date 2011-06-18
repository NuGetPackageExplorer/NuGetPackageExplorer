using System.Collections.Generic;

namespace NuGetPackageExplorer.Types {
    public interface ISourceSettings {
        IList<string> GetSources();
        void SetSources(IEnumerable<string> sources);
        string DefaultSource { get; }
        string ActiveSource { get; set; }
    }
}