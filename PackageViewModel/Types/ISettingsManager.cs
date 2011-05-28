using System.Collections.Generic;

namespace NuGetPackageExplorer.Types {

    public interface ISettingsManager {
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Design", "CA1024:UsePropertiesWhereAppropriate")]
        IList<string> GetMruFiles();
        void SetMruFiles(IEnumerable<string> files);

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Design", "CA1024:UsePropertiesWhereAppropriate")]
        IList<string> GetMruPackageSources();
        void SetMruPackageSources(IEnumerable<string> sources);

        string ActivePackageSource { get; set; }
        string PublishPackageLocation { get; set; }

        string ReadApiKeyFromSettingFile();
        void WriteApiKeyToSettingFile(string apiKey);

        bool ShowLatestVersionOfPackage { get; set; }
    }
}