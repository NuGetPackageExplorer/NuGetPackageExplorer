using System.ComponentModel.Composition;
using System.Globalization;


using NupkgExplorer.Client.Data;
using NupkgExplorer.Framework.Extensions;
using NupkgExplorer.Framework.Json;
using NupkgExplorer.Framework.Query;

namespace NupkgExplorer.Client.Impl
{
    [Export(typeof(INugetEndpoint))]
    public class NugetEndpoint : ApiEndpointBase, INugetEndpoint
    {
        public Task<Json<SearchResponse>> Search(string? search = null, int skip = 0, int take = 25, bool prerelease = false)
        {
            // https://docs.microsoft.com/en-us/nuget/api/search-query-service-resource
            return QueryJson<SearchResponse>(query => query
                .Get()
                .FromUrl("https://azuresearch-usnc.nuget.org/query")
                .Param("q", search!, () => !string.IsNullOrEmpty(search))
                .Param("skip", skip.ToString(CultureInfo.InvariantCulture))
                .Param("take", take.ToString(CultureInfo.InvariantCulture))
                .Param("prerelease", prerelease.ToString())
                .Param("semVerLevel", "2.0.0")
            );
        }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Globalization", "CA1308:Normalize strings to uppercase", Justification = "It's what the URL needs to be")]
        public Task<Json<PackageVersionsResponse>> ListVersions(string packageId)
        {
            ArgumentNullException.ThrowIfNullOrWhiteSpace(packageId);

            packageId = packageId.ToLowerInvariant();

            return QueryJson<PackageVersionsResponse>(query => query
                .Get()
                .FromUrl($"https://api.nuget.org/v3-flatcontainer/{packageId}/index.json")
            );
        }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Globalization", "CA1308:Normalize strings to uppercase", Justification = "It's what the URL needs to be")]
        public async Task<Stream> DownloadPackage(string packageId, string version)
        {
            ArgumentNullException.ThrowIfNullOrWhiteSpace(packageId);
            ArgumentNullException.ThrowIfNullOrWhiteSpace(version);

            packageId = packageId.ToLowerInvariant();
            version = version.ToLowerInvariant();

            // https://docs.microsoft.com/en-us/nuget/api/package-base-address-resource
            return await Query(query => query
                .Get()
                .FromUrl($"https://api.nuget.org/v3-flatcontainer/{packageId}/{version}/{packageId}.{version}.nupkg")
            ).ReadAsStreamAsync();
        }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Globalization", "CA1308:Normalize strings to uppercase", Justification = "It's what the URL needs to be")]
        public async Task DownloadPackage(CancellationToken ct, string packageId, string version, Stream destination, IProgress<(long ReceivedBytes, long? TotalBytes)> progress)
        {
            ArgumentNullException.ThrowIfNullOrWhiteSpace(packageId);
            ArgumentNullException.ThrowIfNullOrWhiteSpace(version);
            ArgumentNullException.ThrowIfNull(destination);
            ArgumentNullException.ThrowIfNull(progress);

            packageId = packageId.ToLowerInvariant();
            version = version.ToLowerInvariant();

            // https://docs.microsoft.com/en-us/nuget/api/package-base-address-resource
            using var response = await Query(query => query
                .Get()
                .FromUrl($"https://api.nuget.org/v3-flatcontainer/{packageId}/{version}/{packageId}.{version}.nupkg")
            ).ConfigureAwait(false);

            var total = response.Content.Headers.ContentLength;
            progress.Report((0, total));
            await response.Content.CopyToAsync(destination, ct).ConfigureAwait(false);
            if (total.HasValue)
            {
                progress.Report((total.Value, total));
            }
        }
    }
}
