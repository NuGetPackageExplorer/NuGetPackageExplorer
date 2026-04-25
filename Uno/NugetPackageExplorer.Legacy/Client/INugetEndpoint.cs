using NupkgExplorer.Client.Data;
using NupkgExplorer.Framework.Json;

namespace NupkgExplorer.Client
{
    public interface INugetEndpoint
    {
        Task<Json<SearchResponse>> Search(string? search = null, int skip = 0, int take = 25, bool prerelease = false);

        Task<Json<PackageVersionsResponse>> ListVersions(string packageId);

        Task<Stream> DownloadPackage(string packageId, string version);

        Task DownloadPackage(string packageId, string version, Stream destination, IProgress<(long ReceivedBytes, long? TotalBytes)> progress, CancellationToken ct);
    }
}
