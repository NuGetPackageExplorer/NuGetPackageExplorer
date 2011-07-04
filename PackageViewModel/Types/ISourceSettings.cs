using System.Collections.Generic;

namespace NuGetPackageExplorer.Types {
    public interface ISourceSettings {
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Design", "CA1024:UsePropertiesWhereAppropriate")]
        IList<string> GetSources();
        void SetSources(IEnumerable<string> sources);
        string DefaultSource { get; }
        string ActiveSource { get; set; }
    }
}