using System;
using System.ComponentModel.Composition;
using NuGet;
using NuGetPackageExplorer.Types;

namespace PackageExplorer {

    [Export(typeof(IPackageChooser))]
    internal class PackageChooserService : IPackageChooser {

        private PackageChooserDialog _dialog;

        [Import]
        public IPackageViewModelFactory ViewModelFactory { get; set; }

        [Import]
        public Lazy<MainWindow> Window { get; set; }

        public PackageInfo SelectPackage() {
            if (_dialog == null) {
                _dialog = new PackageChooserDialog(ViewModelFactory.CreatePackageChooserViewModel()) {
                    Owner = Window.Value
                };
            }
            _dialog.ShowDialog();
            return _dialog.SelectedPackage;
        }

        public void Dispose() {
            if (_dialog != null) {
                _dialog.ForceClose();
            }
        }
    }
}