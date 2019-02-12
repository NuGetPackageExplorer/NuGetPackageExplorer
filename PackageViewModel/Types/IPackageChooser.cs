using System;
using NuGet.Protocol.Core.Types;
using NuGetPe;

namespace NuGetPackageExplorer.Types
{
    public interface IPackageChooser : IDisposable
    {
        SourceRepository? Repository { get; }
        PackageInfo? SelectPackage(string? searchTerm);
        SourceRepository? PluginRepository { get; }
        PackageInfo? SelectPluginPackage();
    }
}
