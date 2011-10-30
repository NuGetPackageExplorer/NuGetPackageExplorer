using System;
using System.ComponentModel.Composition;
using NuGet;
using NuGetPackageExplorer.Types;
using PackageExplorerViewModel;

namespace PackageExplorer {
    [Export(typeof(IPackageChooser))]
    internal class PackageChooserService : IPackageChooser {

        private PackageChooserDialog _dialog;
        private PackageChooserViewModel _viewModel;
        private PackageChooserDialog _pluginDialog;
        private PackageChooserViewModel _pluginViewModel;

        [Import]
        public IPackageViewModelFactory ViewModelFactory { get; set; }

        [Import]
        public Lazy<MainWindow> Window { get; set; }

        public PackageInfo SelectPackage(string searchTerm) {
            if (_dialog == null) {
                _viewModel = ViewModelFactory.CreatePackageChooserViewModel(null);
                _dialog = new PackageChooserDialog(_viewModel) {
                    Owner = Window.Value
                };
            }

            _dialog.ShowDialog(searchTerm);
            return _dialog.SelectedPackage;
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
            return _pluginDialog.SelectedPackage;
        }

        public void Dispose() {
            if (_dialog != null) {
                _dialog.ForceClose();
                _viewModel.Dispose();
            }
        }
    }
}