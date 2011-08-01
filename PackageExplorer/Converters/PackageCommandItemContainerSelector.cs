using System.Windows;
using System.Windows.Controls;
using NuGetPackageExplorer.Types;
using LazyPackageCommand = System.Lazy<NuGetPackageExplorer.Types.IPackageCommand, PackageExplorerViewModel.IPackageCommandMetadata>;

namespace PackageExplorer {
    public class PackageCommandItemContainerSelector : StyleSelector {

        public Style PackageCommandStyle { get; set; }

        public override System.Windows.Style SelectStyle(object item, System.Windows.DependencyObject container) {
            return (item is LazyPackageCommand || container is Separator) ? PackageCommandStyle : null;
        }
    }
}
