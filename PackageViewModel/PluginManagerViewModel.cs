using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Net.NetworkInformation;
using System.Threading.Tasks;
using NuGetPackageExplorer.Types;
using NuGetPe;

namespace PackageExplorerViewModel
{
    public class PluginManagerViewModel : INotifyPropertyChanged, IComparer<PluginInfo>
    {
        private readonly IPackageChooser _packageChooser;
        private readonly INuGetPackageDownloader _packageDownloader;
        private readonly IPluginManager _pluginManager;
        private readonly IUIServices _uiServices;

        public PluginManagerViewModel(
            IPluginManager pluginManager,
            IUIServices uiServices,
            IPackageChooser packageChooser,
            INuGetPackageDownloader packageDownloader)
        {
            _pluginManager = pluginManager ?? throw new ArgumentNullException("pluginManager");
            _uiServices = uiServices ?? throw new ArgumentNullException("uiServices");
            _packageChooser = packageChooser ?? throw new ArgumentNullException("packageChooser");
            _packageDownloader = packageDownloader ?? throw new ArgumentNullException("packageDownloader");

            DeleteCommand = new RelayCommand<PluginInfo>(DeleteCommandExecute, DeleteCommandCanExecute);
            AddCommand = new RelayCommand<string>(AddCommandExecute);

            Plugins = new SortedCollection<PluginInfo>(_pluginManager.Plugins, this);
        }

        public ICollection<PluginInfo> Plugins
        {
            get;
        }

        public RelayCommand<PluginInfo> DeleteCommand { get; private set; }

        public RelayCommand<string> AddCommand { get; private set; }

        #region IComparer<PluginInfo> Members

        public int Compare(PluginInfo x, PluginInfo y)
        {
            var result = string.Compare(x.Id, y.Id, StringComparison.CurrentCultureIgnoreCase);
            if (result != 0)
            {
                return result;
            }

            return x.Version.CompareTo(y.Version);
        }

        #endregion

        #region INotifyPropertyChanged Members

        public event PropertyChangedEventHandler PropertyChanged = delegate { };

        #endregion

        private async void AddCommandExecute(string parameter)
        {
            if (parameter == "Local")
            {
                AddLocalPlugin();
            }
            else if (parameter == "Remote")
            {
                await AddFeedPlugin();
            }
        }

        private async Task AddFeedPlugin()
        {
            if (!NetworkInterface.GetIsNetworkAvailable())
            {
                _uiServices.Show(Resources.NoNetworkConnection, MessageLevel.Warning);
                return;
            }

            var selectedPackageInfo = _packageChooser.SelectPluginPackage();
            if (selectedPackageInfo != null)
            {
                var repository = _packageChooser.PluginRepository;
                if (repository != null)
                {
                    var package = await _packageDownloader.Download(
                    repository,
                    selectedPackageInfo.Identity);

                    if (package != null)
                    {
                        AddSelectedPluginPackage(package);
                    }
                }
            }
        }

        private void AddLocalPlugin()
        {
            var result = _uiServices.OpenFileDialog(
                "Select Plugin Package",
                "NuGet package (*.nupkg)|*.nupkg",
                out var selectedFile);

            if (result)
            {
                try
                {
                    AddSelectedPluginPackage(new ZipPackage(selectedFile));
                }
                catch (Exception e)
                {
                    _uiServices.Show(e.Message, MessageLevel.Error);
                }

            }
        }

        private void AddSelectedPluginPackage(IPackage selectedPackage)
        {
            var packageInfo = _pluginManager.AddPlugin(selectedPackage);
            if (packageInfo != null)
            {
                Plugins.Add(packageInfo);
            }
        }

        private void DeleteCommandExecute(PluginInfo file)
        {
            var confirmed = _uiServices.Confirm(
                "Confirm deleting " + file,
                Resources.ConfirmToDeletePlugin,
                isWarning: true);

            if (!confirmed)
            {
                return;
            }

            var succeeded = _pluginManager.DeletePlugin(file);
            if (succeeded)
            {
                Plugins.Remove(file);
            }
        }

        private bool DeleteCommandCanExecute(PluginInfo file)
        {
            return file != null;
        }
    }
}
