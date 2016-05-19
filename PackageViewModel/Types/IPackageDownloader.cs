using System;
using System.Threading.Tasks;
using NuGetPe;

namespace NuGetPackageExplorer.Types
{
    public interface IPackageDownloader
    {
        Task<IPackage> Download(Uri downloadUri, string packageId, string packageVersion);
        Task Download(string targetFilePath, Uri downloadUri, string packageId, string packageVersion);
    }
}