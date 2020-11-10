using System;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Security;
using NuGet.Versioning;

#if WINDOWS
using OSVersionHelper;
using Windows.Storage;
#endif

namespace NuGetPe
{
    /// <summary>
    /// The machine cache represents a location on the machine where packages are cached. It is a specific implementation of a local repository and can be used as such.
    /// </summary>
    public class MachineCache
    {
        // Maximum number of packages that can live in this cache.
        private const int MaxNumberOfPackages = 100;
        private const string NuGetCachePathEnvironmentVariable = "NuGetCachePath";

        // Disable caching since we don't want to cache packages in memory
        private MachineCache()
        {
            Source = new DirectoryInfo(GetCachePath());
            if (!Source.Exists)
            {
                Source.Create();
            }
        }

        public static MachineCache Default { get; } = new MachineCache();

        public DirectoryInfo Source { get; }

        public ISignaturePackage? FindPackage(string packageId, NuGetVersion version)
        {
            if (version is null)
                throw new ArgumentNullException(nameof(version));
            var path = GetPackageFilePath(packageId, version);

            if (File.Exists(path))
            {
                return new ZipPackage(path);
            }
            else
            {
                return null;
            }
        }

        public void AddPackage(IPackage package)
        {
            if (package is null)
                throw new ArgumentNullException(nameof(package));
            // if the package is already present in the cache, no need to do anything
            using var pkg = FindPackage(package.Id, package.Version);
            if (pkg != null)
            {
                return;
            }

            // don't want to blow up user's hard drive with too many packages
            ClearCache(MaxNumberOfPackages);

            // now copy the package to the cache
            var filePath = GetPackageFilePath(package.Id, package.Version);
            using Stream stream = package.GetStream(),
                          fileStream = File.Create(filePath);
            if (stream != null)
            {
                stream.CopyTo(fileStream);
            }
        }

        private void ClearCache(int threshold)
        {
            try
            {
                // If we exceed the package count then clear the cache
                var packageFiles = Source.GetFiles("*" + Constants.PackageExtension,
                                                   SearchOption.TopDirectoryOnly)
                                         .Concat(Source.GetFiles("*" + Constants.SymbolPackageExtension,
                                                                 SearchOption.TopDirectoryOnly))
                                         .ToList();

                var totalFileCount = packageFiles.Count;
                if (totalFileCount >= threshold)
                {
                    foreach (var packageFile in packageFiles)
                    {
                        try
                        {
                            if (packageFile.Exists)
                            {
                                packageFile.Delete();
                            }
                        }
                        catch (IOException)
                        {
                        }
                        catch (SecurityException)
                        {
                        }
                        catch (UnauthorizedAccessException)
                        {
                        }
                    }
                }
            }
            catch (IOException)
            {
            }

        }

        public bool Clear()
        {
            ClearCache(threshold: 0);
            return true;
        }

        private string GetPackageFilePath(string id, NuGetVersion version)
        {
            return Path.Combine(Source.FullName, id + "." + version.ToNormalizedString() + Constants.PackageExtension);
        }

        /// <summary>
        /// Determines the cache path to use for NuGet.exe. By default, NuGet caches files under %LocalAppData%\NuGet\Cache.
        /// This path can be overridden by specifying a value in the NuGetCachePath environment variable.
        /// </summary>
        private static string GetCachePath()
        {
#if WINDOWS
            // Try getting it from the app model first
            if (WindowsVersionHelper.HasPackageIdentity)
            {
                try
                {
                    return GetCachePathFromLocalCache();
                }
                catch
                {
                    // Don't care here, not on Win7 or running in an app model context
                }
            }
#endif

            return GetCachePath(Environment.GetEnvironmentVariable, Environment.GetFolderPath);
        }

#if WINDOWS
        // Don't load these types inline
        [MethodImpl(MethodImplOptions.NoInlining)]
        private static string GetCachePathFromLocalCache()
        {
            // Get the localized special folder for local app data
            var local = new DirectoryInfo(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData)).Name;
            return GetCachePath(Environment.GetEnvironmentVariable, _ => Path.Combine(ApplicationData.Current.LocalCacheFolder.Path, local));
        }
#endif

        private static string GetCachePath(Func<string, string?> getEnvironmentVariable, Func<Environment.SpecialFolder, string> getFolderPath)
        {
            var cacheOverride = getEnvironmentVariable(NuGetCachePathEnvironmentVariable);
            if (!string.IsNullOrEmpty(cacheOverride))
            {
                return cacheOverride;
            }
            else
            {
                var localAppDataPath = getFolderPath(Environment.SpecialFolder.LocalApplicationData);                
                return Path.Combine(localAppDataPath, "NuGet", "Cache");
            }
        }
    }
}
