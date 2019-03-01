using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel.Composition;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Linq;
using NuGet.Packaging;
using NuGet.Versioning;
using NuGetPackageExplorer.Types;

namespace PackageExplorerViewModel
{
    [Export(typeof(IMruManager))]
    internal class MruManager : IMruManager
    {
        private const int MaxFile = 10;
        private readonly ISettingsManager _settingsManager;

        [SuppressMessage(
            "Microsoft.Performance",
            "CA1811:AvoidUncalledPrivateCode",
            Justification = "Called by MEF")]
        [ImportingConstructor]
        public MruManager(ISettingsManager settingsManager)
        {
            Files = new ObservableCollection<MruItem>();

            _settingsManager = settingsManager;

            try
            {
                var savedFiles = settingsManager.GetMruFiles();
                for (var i = savedFiles.Count - 1; i >= 0; --i)
                {
                    var s = savedFiles[i];
                    var item = ConvertStringToMruItem(s);
                    if (item != null)
                    {
                        AddFile(item);
                    }
                }
            }
            catch // Corrupt setting
            {
                try
                {
                    // try to clear
                    settingsManager.SetMruFiles(Enumerable.Empty<string>());
                }
                catch
                {
                    // something else happened, not much we can do
                }
            }
        }

        public ObservableCollection<MruItem> Files { get; }

        [SuppressMessage(
            "Microsoft.Globalization",
            "CA1308:NormalizeStringsToUppercase",
            Justification = "We don't want to show upper case path.")]
        public void NotifyFileAdded(IPackageMetadata package, string filepath, PackageType packageType)
        {
            var item = new MruItem
            {
                Path = filepath,
                Id = package.Id,
                Version = package.Version,
                PackageType = packageType
            };
            AddFile(item);
        }

        public void Clear()
        {
            Files.Clear();
        }

        public void Dispose()
        {
            OnApplicationExit();
        }

        private void OnApplicationExit()
        {
            var sc = new List<string>();
            foreach (var item in Files)
            {
                if (item != null)
                {
                    var s = ConvertMruItemToString(item);
                    sc.Add(s);
                }
            }
            _settingsManager.SetMruFiles(sc);
        }

        private void AddFile(MruItem mruItem)
        {
            if (mruItem == null)
            {
                throw new ArgumentNullException("mruItem");
            }

            Files.Remove(mruItem);
            Files.Insert(0, mruItem);

            if (Files.Count > MaxFile)
            {
                Files.RemoveAt(Files.Count - 1);
            }
        }

        private static string ConvertMruItemToString(MruItem item)
        {
            // in v1.0, we stored MruItem as "{path}|{package name}|{package type}"
            return string.Format(CultureInfo.InvariantCulture, "{0}|{1}|{2}|{3}", item.Id, item.Version, item.Path,
                                 item.PackageType);
        }

        [SuppressMessage(
            "Microsoft.Performance",
            "CA1811:AvoidUncalledPrivateCode",
            Justification = "Called by MEF.")]
        private static MruItem? ConvertStringToMruItem(string s)
        {
            if (string.IsNullOrEmpty(s))
            {
                return null;
            }

            var parts = s.Split('|');
            if (parts.Length != 4)
            {
                return null;
            }

            for (var i = 0; i < parts.Length; i++)
            {
                if (string.IsNullOrEmpty(parts[i]))
                {
                    return null;
                }
            }

            return ParseMruItem(parts);
        }

        [SuppressMessage(
            "Microsoft.Globalization",
            "CA1308:NormalizeStringsToUppercase",
            Justification = "We don't want to show upper case path.")]
        [SuppressMessage(
            "Microsoft.Performance",
            "CA1811:AvoidUncalledPrivateCode",
            Justification = "Called by MEF.")]
        private static MruItem? ParseMruItem(string[] parts)
        {
            // v1.1 onwards
            if (!Enum.TryParse(parts[3], out PackageType type))
            {
                return null;
            }

            if (!NuGetVersion.TryParse(parts[1], out var version))
            {
                return null;
            }

            return new MruItem
            {
                Id = parts[0],
                Version = version,
                Path = parts[2],
                PackageType = type
            };
        }
    }
}
