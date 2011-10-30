using System;
using System.Collections.Generic;
using System.ComponentModel;
using NuGet;
using NuGetPackageExplorer.Types;

namespace PackageExplorerViewModel {
    public class PluginManagerViewModel : INotifyPropertyChanged, IComparer<PluginInfo> {
        private SortedCollection<PluginInfo> _plugins;
        private readonly IPluginManager _pluginManager;
        private readonly IPackageChooser _packageChooser;
        private readonly IUIServices _uiServices;
        private readonly IPackageDownloader _packageDownloader;

        public PluginManagerViewModel(
            IPluginManager pluginManager, 
            IUIServices uiServices, 
            IPackageChooser packageChooser,
            IPackageDownloader packageDownloader) {
            if (pluginManager == null) {
                throw new ArgumentNullException("pluginManager");
            }

            if (packageChooser == null)
            {
                throw new ArgumentNullException("packageChooser");
            }

            if (uiServices == null)
            {
                throw new ArgumentNullException("uiServices");
            }

            if (packageDownloader == null)
            {
                throw new ArgumentNullException("packageDownloader");
            }

            _pluginManager = pluginManager;
            _uiServices = uiServices;
            _packageChooser = packageChooser;
            _packageDownloader = packageDownloader;

            DeleteCommand = new RelayCommand<PluginInfo>(DeleteCommandExecute, DeleteCommandCanExecute);
            AddCommand = new RelayCommand<string>(AddCommandExecute);
        }

        public event PropertyChangedEventHandler PropertyChanged = delegate { };

        public ICollection<PluginInfo> Plugins {
            get {
                if (_plugins == null) {
                    _plugins = new SortedCollection<PluginInfo>(_pluginManager.GetAllPlugins(), this);
                }
                return _plugins;
            }
        }

        public RelayCommand<PluginInfo> DeleteCommand { get; private set; }

        public RelayCommand<string> AddCommand { get; private set; }

        private void AddCommandExecute(string parameter) {
            if (parameter == "Local") {
                AddLocalPlugin();
            }
            else if (parameter == "Remote") {
                AddFeedPlugin();
            }
        }

        private void AddFeedPlugin()
        {
            var selectedPackageInfo = _packageChooser.SelectPluginPackage();
            if (selectedPackageInfo != null)
            {
                _packageDownloader.Download(
                    selectedPackageInfo.DownloadUrl,
                    selectedPackageInfo.Id,
                    new SemanticVersion(selectedPackageInfo.Version),
                    AddSelectedPluginPackage);
            }
        }

        private void AddLocalPlugin() {
            string selectedFile;
            bool result = _uiServices.OpenFileDialog(
                "Select Plugin Package",
                "NuGet package (*.nupkg)|*.nupkg",
                out selectedFile);

            if (result) {
                AddSelectedPluginPackage(new ZipPackage(selectedFile));
            }
        }

        private void AddSelectedPluginPackage(IPackage selectedPackage)
        {
            PluginInfo packageInfo = _pluginManager.AddPlugin(selectedPackage);
            if (packageInfo != null)
            {
                Plugins.Add(packageInfo);
            }
        }

        private void DeleteCommandExecute(PluginInfo file) {
            bool confirmed = _uiServices.Confirm(
                "Confirm deleting " + file.ToString(),
                Resources.ConfirmToDeletePlugin, 
                isWarning: true);

            if (!confirmed) {
                return;
            }

            bool succeeded = _pluginManager.DeletePlugin(file);
            if (succeeded) {
                Plugins.Remove(file);
            }
        }

        private bool DeleteCommandCanExecute(PluginInfo file) {
            return file != null;
        }

        public int Compare(PluginInfo x, PluginInfo y) {
            int result = String.Compare(x.Id, y.Id, StringComparison.CurrentCultureIgnoreCase);
            if (result != 0)
            {
                return result;
            }

            return x.Version.CompareTo(y.Version);
        }
    }
}