using System;
using System.Collections.ObjectModel;
using System.ComponentModel.Composition;
using NuGetPackageExplorer.Types;

namespace PackageExplorerViewModel
{

    [Export(typeof(IMruPackageSourceManager))]
    public sealed class MruPackageSourceManager : IMruPackageSourceManager
    {
        private const string NuGetFeed = "https://go.microsoft.com/fwlink/?LinkID=206669";

        private const int MaxItem = 5;
        private readonly ObservableCollection<string> _sources = new ObservableCollection<string>();
        private readonly ISettingsManager _settingsManager;

        [ImportingConstructor]
        public MruPackageSourceManager(ISettingsManager settingsManager) {
            _settingsManager = settingsManager;
            LoadDataFromSettings();
        }

        private void LoadDataFromSettings()
        {
            var savedFiles = _settingsManager.GetMruPackageSources();
            for (int i = savedFiles.Count - 1; i >= 0; --i)
            {
                string s = savedFiles[i];
                if (s != null)
                {
                    AddSource(s);
                }
            }

            if (!String.IsNullOrEmpty(_settingsManager.ActivePackageSource))
            {
                AddSource(_settingsManager.ActivePackageSource);
                ActivePackageSource = _settingsManager.ActivePackageSource;
            }

            // if there is no source (this happens after upgrading), add NuGetFeed to it
            if (_sources.Count == 0 || !_sources.Contains(NuGetFeed))
            {
                _sources.Insert(0, NuGetFeed);
            }

            if (String.IsNullOrEmpty(ActivePackageSource))
            {
                // assign the active package source to the first one if it's not already assigned
                ActivePackageSource = _sources[0];
            }
        }

        public string ActivePackageSource
        {
            get;
            set;
        }

        public void OnApplicationExit() {
            _settingsManager.SetMruPackageSources(_sources);
            _settingsManager.ActivePackageSource = ActivePackageSource;
        }

        public ObservableCollection<string> PackageSources {
            get {
                return _sources;
            }
        }

        public void NotifyPackageSourceAdded(string newSource) {
            AddSource(newSource);
        }

        public void Clear()
        {
            _sources.Clear();
        }

        private void AddSource(string newSource) {
            if (newSource == null) {
                throw new ArgumentNullException("newSource");
            }

            if (newSource.Equals(NuGetFeed, StringComparison.OrdinalIgnoreCase))
            {
                return;
            }

            _sources.Remove(NuGetFeed);
            
            SmartRemove(newSource);
            _sources.Insert(0, newSource);

            if (_sources.Count > MaxItem) {
                _sources.RemoveAt(_sources.Count - 1);
            }

            _sources.Insert(0, NuGetFeed);
        }

        private void SmartRemove(string item)
        {
            for (int i = 0; i < _sources.Count; i++)
            {
                if (_sources[i].Equals(item, StringComparison.OrdinalIgnoreCase))
                {
                    _sources.RemoveAt(i);
                    return;
                }
            }
        }
    }
}