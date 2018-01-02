using System;
using System.ComponentModel.Composition;
using NuGetPe;
using NuGetPackageExplorer.Types;
using PackageExplorerViewModel;

namespace PackageExplorer
{
    [Export(typeof(IPackageChooser))]
    internal class PackageChooserService : IPackageChooser
    {
        // for select package dialog
        private PackageChooserDialog _dialog;
        private PackageChooserViewModel _viewModel;

        // for select plugin dialog
        private PackageChooserDialog _pluginDialog;
        private PackageChooserViewModel _pluginViewModel;

        [Import]
        public IPackageViewModelFactory ViewModelFactory { get; set; }

        [Import]
        public IUIServices UIServices { get; set; }

        [Import]
        public INuGetPackageDownloader PackageDownloader { get; set; }

        [Import]
        public Lazy<MainWindow> Window { get; set; }

        #region IPackageChooser Members

        public PackageInfo SelectPackage(string searchTerm)
        {
            if (_dialog == null)
            {
                _viewModel = ViewModelFactory.CreatePackageChooserViewModel(null);
                _viewModel.PackageDownloadRequested += OnPackageDownloadRequested;
                _dialog = new PackageChooserDialog(_viewModel)
                          {
                              Owner = Window.Value
                          };
            }

            _dialog.ShowDialog(searchTerm);
            return _viewModel.SelectedPackage;
        }

        private async void OnPackageDownloadRequested(object sender, EventArgs e)
        {
            var packageInfo = _viewModel.SelectedPackage;
            if (packageInfo != null)
            {

                var packageName = packageInfo.Id + "." + packageInfo.Version.ToString() + NuGetPe.Constants.PackageExtension;
                var title = "Save " + packageName;
                const string filter = "NuGet package file (*.nupkg)|*.nupkg|All files (*.*)|*.*";

                var accepted = UIServices.OpenSaveFileDialog(
                    title,
                    packageName,
                    null,
                    filter,
                    overwritePrompt: true,
                    selectedFilePath: out var selectedFilePath,
                    selectedFilterIndex: out var selectedIndex);

                if (accepted)
                {
                    if (selectedIndex == 1 &&
                        !selectedFilePath.EndsWith(NuGetPe.Constants.PackageExtension, StringComparison.OrdinalIgnoreCase))
                    {
                        selectedFilePath += NuGetPe.Constants.PackageExtension;
                    }

                    await PackageDownloader.Download(selectedFilePath, packageInfo.DownloadUrl, packageInfo.Id, packageInfo.Version);                    
                }
            }
        }

        public PackageInfo SelectPluginPackage()
        {
            if (_pluginDialog == null)
            {
                _pluginViewModel = ViewModelFactory.CreatePackageChooserViewModel(NuGetConstants.PluginFeedUrl);
                _pluginDialog = new PackageChooserDialog(_pluginViewModel)
                                {
                                    Owner = Window.Value
                                };
            }

            _pluginDialog.ShowDialog();
            return _pluginViewModel.SelectedPackage;
        }

        public void Dispose()
        {
            if (_dialog != null)
            {
                _dialog.ForceClose();
                _viewModel.Dispose();
            }
        }

        #endregion
    }
}