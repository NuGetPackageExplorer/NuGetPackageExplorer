using NuGetPe;
using PackageExplorerViewModel;
using System.Threading.Tasks;

namespace NuGetPackageExplorer.Types
{
    public interface IPackageViewModelFactory
    {
        Task<PackageViewModel> CreateViewModel(IPackage package, string packageSource);
        PackageChooserViewModel CreatePackageChooserViewModel(string fixedPackageSource);
        PluginManagerViewModel CreatePluginManagerViewModel();
    }
}