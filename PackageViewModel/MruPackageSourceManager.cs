using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using NuGetPackageExplorer.Types;

namespace PackageExplorerViewModel
{
    public sealed class MruPackageSourceManager : IDisposable
    {
        private const int MaxItem = 5;
        private readonly ISourceSettings _sourceSettings;
        private readonly ObservableCollection<string> _sources = new ObservableCollection<string>();

        public MruPackageSourceManager(ISourceSettings sourceSettings)
        {
            _sourceSettings = sourceSettings;
            LoadDataFromSettings();
        }

        public string ActivePackageSource { get; set; }

        public ObservableCollection<string> PackageSources
        {
            get { return _sources; }
        }

        #region IDisposable Members

        public void Dispose()
        {
            _sourceSettings.SetSources(_sources);
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
            if (_sources.Count == 0 || !_sources.Contains(_sourceSettings.DefaultSource))
            {
                _sources.Insert(0, _sourceSettings.DefaultSource);
            }

            if (string.IsNullOrEmpty(ActivePackageSource))
            {
                // assign the active package source to the first one if it's not already assigned
                ActivePackageSource = _sources[0];
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

            _sources.Remove(defaultFeed);

            SmartRemove(newSource);
            _sources.Insert(0, newSource);

            if (_sources.Count > MaxItem)
            {
                _sources.RemoveAt(_sources.Count - 1);
            }

            _sources.Insert(0, defaultFeed);
        }

        private void SmartRemove(string item)
        {
            for (var i = 0; i < _sources.Count; i++)
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