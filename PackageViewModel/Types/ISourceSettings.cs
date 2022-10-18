using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;

namespace NuGetPackageExplorer.Types
{
    public interface ISourceSettings
    {
        string DefaultSource { get; }
        string ActiveSource { get; set; }

        IList<string> GetSources();

        void SetSources(IEnumerable<string> sources);
    }
}
