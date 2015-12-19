using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.ComponentModel.Composition;
using System.Linq;
using NuGet;
using NuGetPackageExplorer.Types;
using PackageExplorer.Properties;

namespace PackageExplorer
{
    [Export(typeof(ISettingsManager))]
    internal class SettingsManager : ISettingsManager
    {
        public const string ApiKeysSectionName = "apikeys";

        #region ISettingsManager Members

        public bool IsFirstTimeAfterUpdate
        {
            get
            {
                return Settings.Default.IsFirstTimeAfterMigrate;
            }
        }

        public IList<string> GetMruFiles()
        {
            StringCollection files = Settings.Default.MruFiles;
            return files == null ? new List<string>() : files.Cast<string>().ToList();
        }

        public void SetMruFiles(IEnumerable<string> files)
        {
            var sc = new StringCollection();
            sc.AddRange(files.ToArray());
            Settings.Default.MruFiles = sc;
        }

        public IList<string> GetPackageSources()
        {
            StringCollection sources = Settings.Default.MruPackageSources;
            List<string> packageSources = (sources == null) ? new List<string>() : sources.Cast<string>().ToList();
            return packageSources;
        }

        public void SetPackageSources(IEnumerable<string> sources)
        {
            var sc = new StringCollection();
            sc.AddRange(sources.ToArray());
            Settings.Default.MruPackageSources = sc;
        }

        public string ActivePackageSource
        {
            get { return Settings.Default.PackageSource; }
            set { Settings.Default.PackageSource = value; }
        }

        public IList<string> GetPublishSources()
        {
            StringCollection sources = Settings.Default.PublishPackageSources;
            List<string> packageSources = (sources == null) ? new List<string>() : sources.Cast<string>().ToList();
            return packageSources;
        }

        public void SetPublishSources(IEnumerable<string> sources)
        {
            var sc = new StringCollection();
            sc.AddRange(sources.ToArray());
            Settings.Default.PublishPackageSources = sc;
        }

        public string ActivePublishSource
        {
            get { return Settings.Default.PublishPackageLocation; }
            set { Settings.Default.PublishPackageLocation = value; }
        }

        public string ReadApiKey(string source)
        {
            var settings = new UserSettings(new PhysicalFileSystem(Environment.CurrentDirectory));
            string key = settings.GetDecryptedValue(ApiKeysSectionName, source);

            if (String.IsNullOrEmpty(key))
            {
                if (IsFirstTimeAfterUpdate && source.Equals(NuGetConstants.V2LegacyFeedUrl, StringComparison.OrdinalIgnoreCase))
                {
                    key = Settings.Default.PublishPrivateKey;
                }
            }

            return key;
        }

        public void WriteApiKey(string source, string apiKey)
        {
            var settings = new UserSettings(new PhysicalFileSystem(Environment.CurrentDirectory));
            settings.SetEncryptedValue(ApiKeysSectionName, source, apiKey);
        }

        public bool ShowPrereleasePackages
        {
            get { return Settings.Default.ShowPrereleasePackages; }
            set { Settings.Default.ShowPrereleasePackages = value; }
        }

        public bool AutoLoadPackages
        {
            get { return Settings.Default.AutoLoadPackages; }
            set { Settings.Default.AutoLoadPackages = value; }
        }

        public bool PublishAsUnlisted
        {
            get
            {
                return Settings.Default.PublishAsUnlisted;
            }
            set
            {
                Settings.Default.PublishAsUnlisted = value;
            }
        }

        #endregion
    }
}