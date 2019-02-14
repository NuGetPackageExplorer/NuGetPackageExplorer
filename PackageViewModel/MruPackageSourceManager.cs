using System;
using System.Collections.ObjectModel;
using NuGetPackageExplorer.Types;

namespace PackageExplorerViewModel
{
    public sealed class MruPackageSourceManager : IDisposable
    {
        private const int MaxItem = 5;
        private readonly ISourceSettings _sourceSettings;

#pragma warning disable CS8618 // Non-nullable field is uninitialized.
        public MruPackageSourceManager(ISourceSettings sourceSettings)
#pragma warning restore CS8618 // Non-nullable field is uninitialized.
        {
            _sourceSettings = sourceSettings;
            LoadDataFromSettings();
        }

        public string ActivePackageSource { get; set; }

        public ObservableCollection<string> PackageSources { get; } = new ObservableCollection<string>();

        #region IDisposable Members

        public void Dispose()
        {
            _sourceSettings.SetSources(PackageSources);
            _sourceSettings.ActiveSource = ActivePackageSource;
        }

        #endregion

        private void LoadDataFromSettings()
        {
            var savedFiles = _sourceSettings.GetSources();
            for (var i = savedFiles.Count - 1; i >= 0; --i)
            {
                var s = savedFiles[i];
                if (s != null)
                {
                    AddSource(s);
                }
            }

            if (!string.IsNullOrEmpty(_sourceSettings.ActiveSource))
            {
                AddSource(_sourceSettings.ActiveSource);
                ActivePackageSource = _sourceSettings.ActiveSource;
            }

            // if there is no source (this happens after upgrading), add the default source to it
            if (PackageSources.Count == 0 || !PackageSources.Contains(_sourceSettings.DefaultSource))
            {
                PackageSources.Insert(0, _sourceSettings.DefaultSource);
            }

            if (string.IsNullOrEmpty(ActivePackageSource))
            {
                // assign the active package source to the first one if it's not already assigned
                ActivePackageSource = PackageSources[0];
            }
        }

        public void NotifyPackageSourceAdded(string newSource)
        {
            AddSource(newSource);
        }

        private void AddSource(string newSource)
        {
            if (newSource == null)
            {
                throw new ArgumentNullException("newSource");
            }

            var defaultFeed = _sourceSettings.DefaultSource;

            if (newSource.Equals(defaultFeed, StringComparison.OrdinalIgnoreCase))
            {
                return;
            }

            PackageSources.Remove(defaultFeed);

            SmartRemove(newSource);
            PackageSources.Insert(0, newSource);

            if (PackageSources.Count > MaxItem)
            {
                PackageSources.RemoveAt(PackageSources.Count - 1);
            }

            PackageSources.Insert(0, defaultFeed);
        }

        private void SmartRemove(string item)
        {
            for (var i = 0; i < PackageSources.Count; i++)
            {
                if (PackageSources[i].Equals(item, StringComparison.OrdinalIgnoreCase))
                {
                    PackageSources.RemoveAt(i);
                    return;
                }
            }
        }
    }
}
