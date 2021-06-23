using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.IO;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using NupkgExplorer.Client.Data;
using NupkgExplorer.Framework.Extensions;
using NupkgExplorer.Framework.Json;
using NupkgExplorer.Framework.Query;

namespace NupkgExplorer.Client.Impl
{
    [Export(typeof(INugetEndpoint))]
	public class NugetEndpoint : ApiEndpointBase, INugetEndpoint
	{
		public Task<Json<SearchResponse>> Search(string search = null, int skip = 0, int take = 25, bool prerelease = false)
		{
			// https://docs.microsoft.com/en-us/nuget/api/search-query-service-resource
			return QueryJson<SearchResponse>(query => query
				.Get()
				.FromUrl("https://azuresearch-usnc.nuget.org/query")
				.Param("q", search, () => !string.IsNullOrEmpty(search))
				.Param("skip", skip.ToString())
				.Param("take", take.ToString())
				.Param("prerelease", prerelease.ToString())
				.Param("semVerLevel", "2.0.0")
			);
		}

		public Task<Json<PackageVersionsResponse>> ListVersions(string packageId)
		{
			packageId = packageId.ToLowerInvariant();

			return QueryJson<PackageVersionsResponse>(query => query
				.Get()
				.FromUrl($"https://api.nuget.org/v3-flatcontainer/{packageId}/index.json")
			);
		}

		public async Task<Stream> DownloadPackage(string packageId, string version)
		{
			packageId = packageId.ToLowerInvariant();
			version = version.ToLowerInvariant();

			// https://docs.microsoft.com/en-us/nuget/api/package-base-address-resource
			return await Query(query => query
				.Get()
				.FromUrl($"https://api.nuget.org/v3-flatcontainer/{packageId}/{version}/{packageId}.{version}.nupkg")
			).ReadAsStreamAsync();
		}

		public async Task<Stream> DownloadPackage(CancellationToken ct, string packageId, string version, IProgress<(long ReceivedBytes, long? TotalBytes)> progress)
		{
			packageId = packageId.ToLowerInvariant();
			version = version.ToLowerInvariant();

			// https://docs.microsoft.com/en-us/nuget/api/package-base-address-resource
			var response = await Query(HttpCompletionOption.ResponseHeadersRead, query => query
				.Get()
				.FromUrl($"https://api.nuget.org/v3-flatcontainer/{packageId}/{version}/{packageId}.{version}.nupkg")
			);

			var total = response.Content.Headers.ContentLength;
			long read = 0, received = 0;
			var buffer = new byte[2 << 12];
			progress.Report((received, total));

			var stream = new MemoryStream((int)(total ?? 0));
			using (var content = await response.Content.ReadAsStreamAsync())
			{
				while ((read = await content.ReadAsync(buffer, 0, buffer.Length)) > 0)
				{
					await stream.WriteAsync(buffer, 0, (int)read);
					progress.Report((received += read, total));
					ct.ThrowIfCancellationRequested();
				}
				progress.Report((received += read, total));
			}

			return stream;
		}
	}
}
