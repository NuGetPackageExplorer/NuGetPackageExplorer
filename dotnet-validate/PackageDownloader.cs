using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

using NuGet.Common;
using NuGet.Configuration;
using NuGet.Packaging.Core;
using NuGet.Protocol;
using NuGet.Protocol.Core.Types;
using NuGet.Versioning;

namespace NuGetPe
{
    internal sealed class NuGetPackageDownloader : IDisposable
    {
        private readonly ILogger _logger;
        private readonly ISettings _settings;
        private readonly SourceCacheContext _sourceCacheContext;

        public NuGetPackageDownloader(TextWriter logTextWriter)
        {
            _logger = new TextWriterLogger(logTextWriter);
            _settings = Settings.LoadDefaultSettings(".");
            _sourceCacheContext = new SourceCacheContext();
        }

        private async Task<PackageIdentity> GetPackageIdentityAsync(string packageId, SourceRepository sourceRepository, CancellationToken cancellationToken)
        {
            var metadataResource = await sourceRepository.GetResourceAsync<MetadataResource>(cancellationToken).ConfigureAwait(false);
            var latestReleaseVersion = await metadataResource.GetLatestVersion(packageId, includePrerelease: false, includeUnlisted: false, _sourceCacheContext, _logger, cancellationToken).ConfigureAwait(false);
            if (latestReleaseVersion != null)
            {
                return new PackageIdentity(packageId, latestReleaseVersion);
            }
            var latestPrereleaseVersion = await metadataResource.GetLatestVersion(packageId, includePrerelease: true, includeUnlisted: false, _sourceCacheContext, _logger, cancellationToken).ConfigureAwait(false);
            if (latestPrereleaseVersion != null)
            {
                return new PackageIdentity(packageId, latestPrereleaseVersion);
            }
            throw new UnavailableException($"The package {packageId} was not found on {sourceRepository.PackageSource}.");
        }

        public async Task<FileInfo> DownloadAsync(string packageId, NuGetVersion? packageVersion, Uri feedUrl, CancellationToken cancellationToken)
        {
            if (feedUrl is null)
                throw new ArgumentNullException(nameof(feedUrl));

            var packageSource = new PackageSource(feedUrl.ToString());
            var sourceRepository = Repository.Factory.GetCoreV3(packageSource);
            PackageIdentity packageIdentity;
            if (packageVersion != null)
                packageIdentity = new PackageIdentity(packageId, packageVersion);
            else
                packageIdentity = await GetPackageIdentityAsync(packageId, sourceRepository, cancellationToken).ConfigureAwait(false);
            var globalPackagesFolder = SettingsUtility.GetGlobalPackagesFolder(_settings);
            var downloadResource = await sourceRepository.GetResourceAsync<DownloadResource>(cancellationToken).ConfigureAwait(false);
            var result = await downloadResource.GetDownloadResourceResultAsync(packageIdentity, new PackageDownloadContext(_sourceCacheContext), globalPackagesFolder, _logger, cancellationToken).ConfigureAwait(false);
            if (result.Status != DownloadResourceResultStatus.Available)
            {
                throw new UnavailableException($"The package {packageIdentity} was not found on {sourceRepository.PackageSource}.");
            }
            if (!(result.PackageStream is FileStream fileStream))
            {
                throw new InvalidOperationException($"The package stream is expected to be a {nameof(FileStream)} but is a {result.PackageStream?.GetType()}.");
            }
            await result.PackageStream.DisposeAsync().ConfigureAwait(false);
            return new FileInfo(fileStream.Name);
        }

        public void Dispose()
        {
            _sourceCacheContext.Dispose();
        }
    }
}
