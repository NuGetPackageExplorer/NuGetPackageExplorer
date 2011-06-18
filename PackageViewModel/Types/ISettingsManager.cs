using System.Collections.Generic;

namespace NuGetPackageExplorer.Types {

    public interface ISettingsManager {
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Design", "CA1024:UsePropertiesWhereAppropriate")]
        IList<string> GetMruFiles();
        void SetMruFiles(IEnumerable<string> files);

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Design", "CA1024:UsePropertiesWhereAppropriate")]
        IList<string> GetPackageSources();
        void SetPackageSources(IEnumerable<string> sources);
        string ActivePackageSource { get; set; }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Design", "CA1024:UsePropertiesWhereAppropriate")]
        IList<string> GetPublishSources();
        void SetPublishSources(IEnumerable<string> sources);
        string ActivePublishSource { get; set; }

        string ReadApiKey(string source);
        void WriteApiKey(string source, string apiKey);

        bool ShowLatestVersionOfPackage { get; set; }
    }
}