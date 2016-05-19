using NuGetPe;

namespace NuGetPackageExplorer.Types
{
    public interface IPackageCommand
    {
        void Execute(IPackage package, string packagePath);
    }
}