using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.ComponentModel.Composition;
using System.Linq;
using NuGet;
using NuGetPackageExplorer.Types;

namespace PackageExplorer {

    [Export(typeof(ISettingsManager))]
    internal class SettingsManager : ISettingsManager {

        public const string ApiKeysSectionName = "apikeys";

        public IList<string> GetMruFiles() {
            var files = Properties.Settings.Default.MruFiles;
            return files == null ? new List<string>() : files.Cast<string>().ToList();
        }

        public void SetMruFiles(IEnumerable<string> files) {
            StringCollection sc = new StringCollection();
            sc.AddRange(files.ToArray());
            Properties.Settings.Default.MruFiles = sc;
        }

        public IList<string> GetMruPackageSources() {
            var sources = Properties.Settings.Default.MruPackageSources;
            var packageSources = (sources == null) ? new List<string>() : sources.Cast<string>().ToList();
            return packageSources;
        }

        public void SetMruPackageSources(IEnumerable<string> sources) {
            StringCollection sc = new StringCollection();
            sc.AddRange(sources.ToArray());
            Properties.Settings.Default.MruPackageSources = sc;
        }

        public string ActivePackageSource {
            get {
                return Properties.Settings.Default.PackageSource;
            }
            set {
                Properties.Settings.Default.PackageSource = value;
            }
        }

        public string PublishPackageLocation {
            get {
                return Properties.Settings.Default.PublishPackageLocation;
            }
            set {
                Properties.Settings.Default.PublishPackageLocation = value;
            }
        }

        public string ReadApiKeyFromSettingFile() {
            var settings = new UserSettings(new PhysicalFileSystem(Environment.CurrentDirectory));
            string key = settings.GetDecryptedValue(ApiKeysSectionName, PublishPackageLocation);
            return key ?? Properties.Settings.Default.PublishPrivateKey;
        }

        public void WriteApiKeyToSettingFile(string apiKey) {
            var settings = new UserSettings(new PhysicalFileSystem(Environment.CurrentDirectory));
            settings.SetEncryptedValue(ApiKeysSectionName, PublishPackageLocation, apiKey);
        }


        public bool ShowLatestVersionOfPackage {
            get {
                return Properties.Settings.Default.ShowLatestVersionOfPackage;
            }
            set {
                Properties.Settings.Default.ShowLatestVersionOfPackage = value;
            }
        }
    }
}