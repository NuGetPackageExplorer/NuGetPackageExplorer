using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;

namespace NuGetPackageExplorer.Types
{
    public interface ISettingsManager
    {
        string ActivePackageSource { get; set; }
        string ActivePublishSource { get; set; }
        bool ShowPrereleasePackages { get; set; }
        bool AutoLoadPackages { get; set; }

        [SuppressMessage("Microsoft.Design", "CA1024:UsePropertiesWhereAppropriate")]
        IList<string> GetMruFiles();
        void SetMruFiles(IEnumerable<string> files);

        [SuppressMessage("Microsoft.Design", "CA1024:UsePropertiesWhereAppropriate")]
        IList<string> GetPackageSources();
        void SetPackageSources(IEnumerable<string> sources);

        [SuppressMessage("Microsoft.Design", "CA1024:UsePropertiesWhereAppropriate")]
        IList<string> GetPublishSources();
        void SetPublishSources(IEnumerable<string> sources);

        string? ReadApiKey(string source);
        void WriteApiKey(string source, string apiKey);
        bool PublishAsUnlisted { get; set; }

        string SigningCertificate { get; set; }
        string? TimestampServer { get; set; }
        string SigningHashAlgorithmName { get; set; }

        int FontSize { get; set; }

        bool ShowTaskShortcuts { get; set; }

        string WindowPlacement { get; set; }

        double PackageChooserDialogWidth { get; set; }
        double PackageChooserDialogHeight { get; set; }

        double PackageContentHeight { get; set; }
        double ContentViewerHeight { get; set; }

        bool WordWrap { get; set; }
        bool ShowLineNumbers { get; set; }
    }
}
