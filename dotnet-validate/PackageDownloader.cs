using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
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
        private readonly PackageSourceMapping _packageSourceMapping;

        public NuGetPackageDownloader(ILogger logger, DirectoryInfo nuGetConfigDirectory)
        {
            _logger = logger;
            _settings = Settings.LoadDefaultSettings(nuGetConfigDirectory.FullName);
            _sourceCacheContext = new SourceCacheContext();
            _packageSourceMapping = PackageSourceMapping.GetPackageSourceMapping(_settings);
        }

        private IReadOnlyCollection<PackageSource> GetPackageSources(string packageId)
        {
            var packageSourceProvider = new PackageSourceProvider(_settings);
            var packageSources = packageSourceProvider.LoadPackageSources().Where(e => e.IsEnabled && e.IsHttp).Distinct().ToList();

            if (_packageSourceMapping.IsEnabled)
            {
                var sourceNames = _packageSourceMapping.GetConfiguredPackageSources(packageId);
                return packageSources.Where(e => sourceNames.Contains(e.Name)).ToList();
            }

            if (packageSources.Count == 0)
            {
                var officialPackageSource = new PackageSource(NuGet.Configuration.NuGetConstants.V3FeedUrl, NuGet.Configuration.NuGetConstants.NuGetHostName);
                packageSources.Add(officialPackageSource);
                var configFilePaths = _settings.GetConfigFilePaths().Distinct();
                _logger.LogWarning($"No enabled remote NuGet sources could be found in {string.Join(", ", configFilePaths)}. Using the fallback {officialPackageSource}");
            }

            return packageSources.ToList();
        }

        private async Task<PackageIdentity?> GetPackageIdentityAsync(string packageId, SourceRepository sourceRepository, CancellationToken cancellationToken)
        {
            var metadataResource = await sourceRepository.GetResourceAsync<MetadataResource>(cancellationToken).ConfigureAwait(false);
            var latestReleaseVersion = await metadataResource.GetLatestVersion(packageId, includePrerelease: false, includeUnlisted: false, _sourceCacheContext, _logger, cancellationToken).ConfigureAwait(false);
            if (latestReleaseVersion is not null)
            {
                return new PackageIdentity(packageId, latestReleaseVersion);
            }
            var latestPrereleaseVersion = await metadataResource.GetLatestVersion(packageId, includePrerelease: true, includeUnlisted: false, _sourceCacheContext, _logger, cancellationToken).ConfigureAwait(false);
            return latestPrereleaseVersion is null ? null : new PackageIdentity(packageId, latestPrereleaseVersion);
        }

        public async Task<FileInfo> DownloadAsync(string packageId, NuGetVersion? packageVersion, CancellationToken cancellationToken)
        {
            var packageSources = GetPackageSources(packageId);
            foreach (var sourceRepository in packageSources.Select(e => Repository.Factory.GetCoreV3(e)))
            {
                PackageIdentity? packageIdentity;
                if (packageVersion is not null)
                    packageIdentity = new PackageIdentity(packageId, packageVersion);
                else
                    packageIdentity = await GetPackageIdentityAsync(packageId, sourceRepository, cancellationToken).ConfigureAwait(false);

                if (packageIdentity is null)
                {
                    continue;
                }

                var globalPackagesFolder = SettingsUtility.GetGlobalPackagesFolder(_settings);
                var downloadResource = await sourceRepository.GetResourceAsync<DownloadResource>(cancellationToken).ConfigureAwait(false);
                var packageDownloadContext = new PackageDownloadContext(_sourceCacheContext, null, false, _packageSourceMapping);
                var result = await downloadResource.GetDownloadResourceResultAsync(packageIdentity, packageDownloadContext, globalPackagesFolder, _logger, cancellationToken).ConfigureAwait(false);
                if (result.Status != DownloadResourceResultStatus.Available)
                {
                    continue;
                }
                if (result.PackageStream is not FileStream fileStream)
                {
                    throw new InvalidOperationException($"The package stream is expected to be a {nameof(FileStream)} but is a {result.PackageStream?.GetType()}.");
                }

                var identity = result.PackageReader is null ? null : await result.PackageReader.GetIdentityAsync(cancellationToken).ConfigureAwait(false);
                if (identity is not null && identity.Version != packageIdentity.Version)
                {
                    _logger.LogWarning($"The manifest/.nuspec version of {packageId} ({identity.Version}) does not match the requested version ({packageIdentity.Version})");
                }

                await result.PackageStream.DisposeAsync().ConfigureAwait(false);
                return new FileInfo(fileStream.Name);
            }

            var packageDisplayName = packageVersion is null ? packageId : $"{packageId} {packageVersion.ToNormalizedString()}";
            string notFoundMessage = packageSources.Count switch
            {
                1 => $"the \"{packageSources.First().Name}\" NuGet package source.",
                _ => $"{packageSources.Skip(1).Aggregate($"neither \"{packageSources.First().Name}\"", (s, p) => s + $" nor \"{p.Name}\"")} NuGet package sources.",
            };
            throw new UnavailableException($"The package \"{packageDisplayName}\" was not found in {notFoundMessage}");
        }

        public void Dispose()
        {
            _sourceCacheContext.Dispose();
        }
    }
}
