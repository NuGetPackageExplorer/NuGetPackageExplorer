using System;
using System.ComponentModel.Composition;
using System.Windows;
using NuGet.Protocol.Core.Types;
using NuGetPackageExplorer.Types;
using NuGetPe;
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

        public SourceRepository Repository => _viewModel.ActiveRepository;

        public PackageInfo SelectPackage(string searchTerm)
        {
            if (_dialog == null)
            {
                _viewModel = ViewModelFactory.CreatePackageChooserViewModel(null);
                _viewModel.PackageDownloadRequested += OnPackageDownloadRequested;
                _dialog = new PackageChooserDialog(_viewModel);
            }

            _dialog.Owner = Window.Value;
            ReCenterPackageChooserDialog(_dialog);
            _dialog.ShowDialog(searchTerm);
            return _viewModel.SelectedPackage;
        }

        private async void OnPackageDownloadRequested(object sender, EventArgs e)
        {
            var repository = _viewModel.ActiveRepository;
            var packageInfo = _viewModel.SelectedPackage;
            if (packageInfo != null)
            {

                var packageName = packageInfo.Id + "." + packageInfo.Version + NuGetPe.Constants.PackageExtension;
                var title = "Save " + packageName;
                const string filter = "NuGet package file (*.nupkg)|*.nupkg|NuGet Symbols package file (*.snupkg)|*.snupkg|All files (*.*)|*.*";

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

                    await PackageDownloader.Download(selectedFilePath, repository, packageInfo.Identity);
                }
            }
        }

        public SourceRepository PluginRepository => _pluginViewModel.ActiveRepository;

        public PackageInfo SelectPluginPackage()
        {
            if (_pluginDialog == null)
            {
                _pluginViewModel = ViewModelFactory.CreatePackageChooserViewModel(NuGetConstants.PluginFeedUrl);
                _pluginDialog = new PackageChooserDialog(_pluginViewModel);
            }

            _pluginDialog.Owner = Window.Value;
            ReCenterPackageChooserDialog(_pluginDialog);
            _pluginDialog.ShowDialog();
            return _pluginViewModel.SelectedPackage;
        }

        private void ReCenterPackageChooserDialog(StandardDialog dialog)
        {
            if (dialog.Owner == null)
            {
                return;
            }

            var ownerCenterX = dialog.Owner.Left + dialog.Owner.Width / 2;
            var ownerCenterY = dialog.Owner.Top + dialog.Owner.Height / 2;

            if (ownerCenterX < dialog.ActualWidth / 2 || ownerCenterY < dialog.ActualHeight / 2)
            {
                dialog.Left = (SystemParameters.WorkArea.Width - dialog.ActualHeight) / 2;
                dialog.Top = (SystemParameters.WorkArea.Height - dialog.ActualHeight) / 2;
                return;
            }

            dialog.Left = ownerCenterX - dialog.ActualWidth / 2;
            dialog.Top = ownerCenterY - dialog.ActualHeight / 2;
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
