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
        private PackageChooserViewModel? _viewModel;

        // for select plugin dialog
        private PackageChooserDialog? _pluginDialog;
        private PackageChooserViewModel? _pluginViewModel;

#pragma warning disable CS8618 // Non-nullable field is uninitialized.
        [Import]
        public IPackageViewModelFactory ViewModelFactory { get; set; }

        [Import]
        public ISettingsManager SettingsManager { get; set; }

        [Import]
        public IUIServices UIServices { get; set; }

        [Import]
        public INuGetPackageDownloader PackageDownloader { get; set; }

        [Import]
        public Lazy<MainWindow> Window { get; set; }

#pragma warning restore CS8618 // Non-nullable field is uninitialized.

        public SourceRepository? Repository => _viewModel?.ActiveRepository;

        public PackageInfo? SelectPackage(string? searchTerm)
        {
            if (_viewModel == null)
            {
                _viewModel = ViewModelFactory.CreatePackageChooserViewModel(null);
                _viewModel.PackageDownloadRequested += OnPackageDownloadRequested;
            }

            var dialog = new PackageChooserDialog(SettingsManager, _viewModel)
            {
                Owner = Window.Value
            };

            ReCenterPackageChooserDialog(dialog);

            try
            {
                dialog.ShowDialog(searchTerm);
            }
            catch (ArgumentException e)
            {
                UIServices.Show(e.Message, MessageLevel.Error);
            }

            return _viewModel.SelectedPackage;
        }

        private async void OnPackageDownloadRequested(object sender, EventArgs e)
        {
            DiagnosticsClient.TrackEvent("PackageChooserService_OnPackageDownloadRequested");

            var vm = (PackageChooserViewModel)sender;
            var repository = vm.ActiveRepository;
            var packageInfo = vm.SelectedPackage;
            if (packageInfo != null && repository != null)
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
                    else if (selectedIndex == 2 &&
                             !selectedFilePath.EndsWith(NuGetPe.Constants.SymbolPackageExtension, StringComparison.OrdinalIgnoreCase))
                    {
                        selectedFilePath += NuGetPe.Constants.SymbolPackageExtension;
                    }

                    try
                    {
                        await PackageDownloader.Download(selectedFilePath, repository, packageInfo.Identity);
                    }
                    catch (Exception ex)
                    {
                        UIServices.Show(ex.Message, MessageLevel.Error);
                    }

                }
            }
        }

        public SourceRepository? PluginRepository => _pluginViewModel?.ActiveRepository;

        public PackageInfo? SelectPluginPackage()
        {
            if (_pluginDialog == null)
            {
                _pluginViewModel = ViewModelFactory.CreatePackageChooserViewModel(NuGetConstants.PluginFeedUrl);
                _pluginDialog = new PackageChooserDialog(SettingsManager, _pluginViewModel);
            }

            _pluginDialog.Owner = Window.Value;
            ReCenterPackageChooserDialog(_pluginDialog);
            _pluginDialog.ShowDialog();
            return _pluginViewModel?.SelectedPackage;
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
            if (_viewModel != null)
            {
                _viewModel.PackageDownloadRequested -= OnPackageDownloadRequested;
                _viewModel.Dispose();
                _viewModel = null;
            }
        }
    }
}
