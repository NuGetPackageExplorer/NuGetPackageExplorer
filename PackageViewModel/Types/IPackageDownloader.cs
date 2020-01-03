using System.Threading.Tasks;
using NuGet.Packaging.Core;
using NuGet.Protocol.Core.Types;
using NuGetPe;

namespace NuGetPackageExplorer.Types
{
    public interface INuGetPackageDownloader
    {
        Task<ISignaturePackage?> Download(SourceRepository sourceRepository, PackageIdentity packageIdentity);
        Task Download(string targetFilePath, SourceRepository sourceRepository, PackageIdentity packageIdentity);
    }
}
