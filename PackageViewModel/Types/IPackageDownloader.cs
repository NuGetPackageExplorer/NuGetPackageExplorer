using System;
using System.Threading.Tasks;
using NuGet;

namespace NuGetPackageExplorer.Types
{
    public interface IPackageDownloader
    {
        Task<IPackage> Download(Uri downloadUri, string packageId, string packageVersion);
        Task Download(string targetFilePath, Uri downloadUri, string packageId, string packageVersion);
    }
}