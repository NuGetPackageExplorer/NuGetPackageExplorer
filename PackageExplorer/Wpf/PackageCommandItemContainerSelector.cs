using System.Windows;
using System.Windows.Controls;
using LazyPackageCommand =
    System.Lazy<NuGetPackageExplorer.Types.IPackageCommand, NuGetPackageExplorer.Types.IPackageCommandMetadata>;

namespace PackageExplorer
{
    public class PackageCommandItemContainerSelector : StyleSelector
    {
        public Style? PackageCommandStyle { get; set; }

        public override Style? SelectStyle(object item, DependencyObject container)
        {
            return (item is LazyPackageCommand || container is Separator) ? PackageCommandStyle : null;
        }
    }
}
