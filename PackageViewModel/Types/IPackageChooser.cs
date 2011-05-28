using System;
using NuGet;

namespace NuGetPackageExplorer.Types {
    public interface IPackageChooser : IDisposable {
        PackageInfo SelectPackage();
    }
}