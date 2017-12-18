using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;

namespace NuGetPackageExplorer.Types
{
    public interface ISettingsManager
    {
        bool IsFirstTimeAfterUpdate { get; }

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

        string ReadApiKey(string source);
        void WriteApiKey(string source, string apiKey);
        bool PublishAsUnlisted { get; set; }
        bool UseApiKey { get; set; }
    }
}