using System;
using System.IO;
using System.Security;
using NuGet.Versioning;

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
        private static readonly MachineCache _default = new MachineCache();
        private readonly string _cacheRoot;

        // Disable caching since we don't want to cache packages in memory
        private MachineCache()
        {
            _cacheRoot = GetCachePath();
        }

        public static MachineCache Default
        {
            get { return _default; }
        }

        public string Source
        {
            get { return _cacheRoot; }
        }

        public ISignaturePackage FindPackage(string packageId, NuGetVersion version)
        {
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
            // if the package is already present in the cache, no need to do anything
            if (FindPackage(package.Id, package.Version) != null)
            {
                return;
            }

            // create the cache directory if it doesn't exist
            var cacheDirectory = new DirectoryInfo(Source);
            if (!cacheDirectory.Exists)
            {
                cacheDirectory.Create();
            }

            // don't want to blow up user's hard drive with too many packages
            ClearCache(cacheDirectory, MaxNumberOfPackages);

            // now copy the package to the cache
            var filePath = GetPackageFilePath(package.Id, package.Version);
            using (Stream stream = package.GetStream(),
                          fileStream = File.Create(filePath))
            {
                if (stream != null)
                {
                    stream.CopyTo(fileStream);
                }
            }
        }

        private static void ClearCache(DirectoryInfo cacheDirectory, int threshold)
        {
            // If we exceed the package count then clear the cache
            var packageFiles = cacheDirectory.GetFiles("*" + Constants.PackageExtension,
                                                              SearchOption.TopDirectoryOnly);
            var totalFileCount = packageFiles.Length;
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

        public bool Clear()
        {
            var dirInfo = new DirectoryInfo(_cacheRoot);
            if (dirInfo.Exists)
            {
                ClearCache(dirInfo, threshold: 0);
                return true;
            }

            return false;
        }

        private string GetPackageFilePath(string id, NuGetVersion version)
        {
            return Path.Combine(Source, id + "." + version.ToNormalizedString() + Constants.PackageExtension);
        }

        /// <summary>
        /// Determines the cache path to use for NuGet.exe. By default, NuGet caches files under %LocalAppData%\NuGet\Cache.
        /// This path can be overridden by specifying a value in the NuGetCachePath environment variable.
        /// </summary>
        private static string GetCachePath()
        {
            return GetCachePath(Environment.GetEnvironmentVariable, Environment.GetFolderPath);
        }

        private static string GetCachePath(Func<string, string> getEnvironmentVariable, Func<Environment.SpecialFolder, string> getFolderPath)
        {
            var cacheOverride = getEnvironmentVariable(NuGetCachePathEnvironmentVariable);
            if (!string.IsNullOrEmpty(cacheOverride))
            {
                return cacheOverride;
            }
            else
            {
                var localAppDataPath = getFolderPath(Environment.SpecialFolder.LocalApplicationData);
                if (string.IsNullOrEmpty(localAppDataPath))
                {
                    return null;
                }
                return Path.Combine(localAppDataPath, "NuGet", "Cache");
            }
        }
    }
}
