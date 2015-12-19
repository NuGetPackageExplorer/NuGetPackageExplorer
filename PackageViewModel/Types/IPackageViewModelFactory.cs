using NuGet;
using PackageExplorerViewModel;

namespace NuGetPackageExplorer.Types
{
    public interface IPackageViewModelFactory
    {
        PackageViewModel CreateViewModel(IPackage package, string packageSource);
        PackageChooserViewModel CreatePackageChooserViewModel(string fixedPackageSource);
        PluginManagerViewModel CreatePluginManagerViewModel();
    }
}