using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel.Composition;
using System.Globalization;
using NuGet;
using NuGetPackageExplorer.Types;

namespace PackageExplorerViewModel {

    [Export(typeof(IMruManager))]
    internal class MruManager : IMruManager {
        private const int MaxFile = 7;
        private readonly ObservableCollection<MruItem> _files;
        private readonly ISettingsManager _settingsManager;

        [System.Diagnostics.CodeAnalysis.SuppressMessage(
            "Microsoft.Performance", 
            "CA1811:AvoidUncalledPrivateCode",
            Justification="Called by MEF")]
        [ImportingConstructor]
        public MruManager(ISettingsManager settingsManager) {
            var savedFiles = settingsManager.GetMruFiles();

            _files = new ObservableCollection<MruItem>();
            for (int i = savedFiles.Count - 1; i >= 0; --i) {
                string s = savedFiles[i];
                MruItem item = ConvertStringToMruItem(s);
                if (item != null) {
                    AddFile(item);
                }
            }

            _settingsManager = settingsManager;
        }

        public void OnApplicationExit() {
            List<string> sc = new List<string>();
            foreach (var item in _files) {
                if (item != null) {
                    string s = ConvertMruItemToString(item);
                    sc.Add(s);
                }
            }
            _settingsManager.SetMruFiles(sc);
        }

        public ObservableCollection<MruItem> Files {
            get {
                return _files;
            }
        }

        [System.Diagnostics.CodeAnalysis.SuppressMessage(
            "Microsoft.Globalization",
            "CA1308:NormalizeStringsToUppercase",
            Justification = "We don't want to show upper case path.")]
        public void NotifyFileAdded(IPackageMetadata package, string filepath, PackageType packageType) {
            var item = new MruItem {
                Path = filepath.ToLowerInvariant(),
                Id = package.Id,
                Version = package.Version,
                PackageType = packageType
            };
            AddFile(item);
        }

        private void AddFile(MruItem mruItem) {
            if (mruItem == null) {
                throw new ArgumentNullException("mruItem");
            }

            _files.Remove(mruItem);
            _files.Insert(0, mruItem);

            if (_files.Count > MaxFile) {
                _files.RemoveAt(_files.Count - 1);
            }
        }

        public void Clear() {
            _files.Clear();
        }

        private static string ConvertMruItemToString(MruItem item) {
            // in v1.0, we stored MruItem as "{path}|{package name}|{package type}"
            return String.Format(CultureInfo.InvariantCulture, "{0}|{1}|{2}|{3}", item.Id, item.Version, item.Path, item.PackageType);
        }

        [System.Diagnostics.CodeAnalysis.SuppressMessage(
            "Microsoft.Performance", 
            "CA1811:AvoidUncalledPrivateCode",
            Justification="Called by MEF.")]
        private static MruItem ConvertStringToMruItem(string s) {
            if (String.IsNullOrEmpty(s)) {
                return null;
            }

            string[] parts = s.Split('|');
            if (parts.Length != 3 && parts.Length != 4) {
                return null;
            }

            for (int i = 0; i < parts.Length; i++) {
                if (String.IsNullOrEmpty(parts[i])) {
                    return null;
                }
            }

            if (parts.Length == 3) {
                return ParseVersion1MruItem(parts);
            }
            else {
                return ParseMruItem(parts);
            }
        }

        [System.Diagnostics.CodeAnalysis.SuppressMessage(
            "Microsoft.Globalization",
            "CA1308:NormalizeStringsToUppercase",
            Justification = "We don't want to show upper case path.")]
        [System.Diagnostics.CodeAnalysis.SuppressMessage(
            "Microsoft.Performance",
            "CA1811:AvoidUncalledPrivateCode",
            Justification = "Called by MEF.")]
        private static MruItem ParseMruItem(string[] parts) {
            // v1.1 onwards
            PackageType type;
            if (!Enum.TryParse<PackageType>(parts[3], out type)) {
                return null;
            }

            Version version;
            if (!Version.TryParse(parts[1], out version)) {
                return null;
            }

            return new MruItem {
                Id = parts[0],
                Version = version,
                Path = parts[2].ToLowerInvariant(),
                PackageType = type
            };
        }

        /// <summary>
        /// legacy format for version v1.0 and before
        /// </summary>
        [System.Diagnostics.CodeAnalysis.SuppressMessage(
            "Microsoft.Globalization",
            "CA1308:NormalizeStringsToUppercase",
            Justification = "We don't want to show upper case path.")]
        [System.Diagnostics.CodeAnalysis.SuppressMessage(
            "Microsoft.Performance",
            "CA1811:AvoidUncalledPrivateCode",
            Justification = "Called by MEF.")]
        private static MruItem ParseVersion1MruItem(string[] parts) {
            // v1.0

            PackageType type;
            if (!Enum.TryParse<PackageType>(parts[2], out type)) {
                return null;
            }

            string packageName = parts[1];
            string[] nameParts = packageName.Split(' ');
            if (nameParts.Length != 2) {
                return null;
            }

            Version version;
            if (!Version.TryParse(nameParts[1], out version)) {
                return null;
            }

            return new MruItem {
                Id = nameParts[0],
                Version = version,
                Path = parts[0].ToLowerInvariant(),
                PackageType = type
            };
        }
    }
}