using System;
using System.Collections.ObjectModel;
using NuGet.Packaging;

namespace NuGetPackageExplorer.Types
{
    public interface IMruManager : IDisposable
    {
        ObservableCollection<MruItem> Files { get; }
        void NotifyFileAdded(IPackageMetadata package, string filePath, PackageType packageType);
        void Clear();
    }
}