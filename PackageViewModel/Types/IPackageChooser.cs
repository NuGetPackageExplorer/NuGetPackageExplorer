using System;
using NuGetPe;

namespace NuGetPackageExplorer.Types
{
    public interface IPackageChooser : IDisposable
    {
        PackageInfo SelectPackage(string searchTerm);
        PackageInfo SelectPluginPackage();
    }
}