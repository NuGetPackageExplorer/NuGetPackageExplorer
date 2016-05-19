using System;
using System.Collections.ObjectModel;
using NuGetPe;

namespace NuGetPackageExplorer.Types
{
    public interface IMruManager : IDisposable
    {
        ObservableCollection<MruItem> Files { get; }
        void NotifyFileAdded(IPackageMetadata package, string filePath, PackageType packageType);
        void Clear();
    }
}