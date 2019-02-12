using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;

namespace NuGetPackageExplorer.Types
{
    public interface ISourceSettings
    {
        string DefaultSource { get; }
        string ActiveSource { get; set; }

        [SuppressMessage("Microsoft.Design", "CA1024:UsePropertiesWhereAppropriate")]
        IList<string> GetSources();

        void SetSources(IEnumerable<string> sources);
    }
}
