﻿using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel.Composition;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using NuGetPe;
using NuGetPackageExplorer.Types;

namespace PackageExplorerViewModel
{
    [Export(typeof(IMruManager))]
    internal class MruManager : IMruManager
    {
        private const int MaxFile = 10;
        private readonly ObservableCollection<MruItem> _files;
        private readonly ISettingsManager _settingsManager;

        [SuppressMessage(
            "Microsoft.Performance",
            "CA1811:AvoidUncalledPrivateCode",
            Justification = "Called by MEF")]
        [ImportingConstructor]
        public MruManager(ISettingsManager settingsManager)
        {
            IList<string> savedFiles = settingsManager.GetMruFiles();

            _files = new ObservableCollection<MruItem>();
            for (int i = savedFiles.Count - 1; i >= 0; --i)
            {
                string s = savedFiles[i];
                MruItem item = ConvertStringToMruItem(s);
                if (item != null)
                {
                    AddFile(item);
                }
            }

            _settingsManager = settingsManager;
        }

        #region IMruManager Members

        public ObservableCollection<MruItem> Files
        {
            get { return _files; }
        }

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
            _files.Clear();
        }

        public void Dispose()
        {
            OnApplicationExit();
        }

        #endregion

        private void OnApplicationExit()
        {
            var sc = new List<string>();
            foreach (MruItem item in _files)
            {
                if (item != null)
                {
                    string s = ConvertMruItemToString(item);
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

            _files.Remove(mruItem);
            _files.Insert(0, mruItem);

            if (_files.Count > MaxFile)
            {
                _files.RemoveAt(_files.Count - 1);
            }
        }

        private static string ConvertMruItemToString(MruItem item)
        {
            // in v1.0, we stored MruItem as "{path}|{package name}|{package type}"
            return String.Format(CultureInfo.InvariantCulture, "{0}|{1}|{2}|{3}", item.Id, item.Version, item.Path,
                                 item.PackageType);
        }

        [SuppressMessage(
            "Microsoft.Performance",
            "CA1811:AvoidUncalledPrivateCode",
            Justification = "Called by MEF.")]
        private static MruItem ConvertStringToMruItem(string s)
        {
            if (String.IsNullOrEmpty(s))
            {
                return null;
            }

            string[] parts = s.Split('|');
            if (parts.Length != 4)
            {
                return null;
            }

            for (int i = 0; i < parts.Length; i++)
            {
                if (String.IsNullOrEmpty(parts[i]))
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
        private static MruItem ParseMruItem(string[] parts)
        {
            // v1.1 onwards
            PackageType type;
            if (!Enum.TryParse(parts[3], out type))
            {
                return null;
            }

            NuGet.SemanticVersion version;
            if (!NuGet.SemanticVersion.TryParse(parts[1], out version))
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