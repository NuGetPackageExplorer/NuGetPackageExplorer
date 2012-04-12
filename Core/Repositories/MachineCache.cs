using System;
using System.IO;
using System.Linq;

namespace NuGet
{
    /// <summary>
    /// The machine cache represents a location on the machine where packages are cached. It is a specific implementation of a local repository and can be used as such.
    /// </summary>
    public class MachineCache : IPackageRepository
    {
        // Maximum number of packages that can live in this cache.
        private const int MaxPackages = 100;
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

        #region IPackageRepository Members

        public bool SupportsPrereleasePackages
        {
            get
            {
                return true;
            }
        }

        public string Source
        {
            get { return _cacheRoot; }
        }

        public IQueryable<IPackage> GetPackages()
        {
            throw new NotSupportedException();
        }

        #endregion

        public IPackage FindPackage(string packageId, SemanticVersion version)
        {
            string path = GetPackageFilePath(packageId, version);

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
            ClearCacheIfFull(cacheDirectory);

            // now copy the package to the cache
            string filePath = GetPackageFilePath(package.Id, package.Version);
            using (Stream stream = package.GetStream(),
                          fileStream = File.Create(filePath))
            {
                if (stream != null)
                {
                    stream.CopyTo(fileStream);
                }
            }
        }

        private static void ClearCacheIfFull(DirectoryInfo cacheDirectory)
        {
            // If we exceed the package count then clear the cache
            FileInfo[] packageFiles = cacheDirectory.GetFiles("*" + Constants.PackageExtension,
                                                              SearchOption.TopDirectoryOnly);
            int totalFileCount = packageFiles.Length;
            if (totalFileCount >= MaxPackages)
            {
                foreach (FileInfo packageFile in packageFiles)
                {
                    try
                    {
                        if (packageFile.Exists)
                        {
                            packageFile.Delete();
                        }
                    }
                    catch (FileNotFoundException)
                    {
                    }
                    catch (UnauthorizedAccessException)
                    {
                    }
                }
            }
        }

        private string GetPackageFilePath(string id, SemanticVersion version)
        {
            return Path.Combine(Source, id + "." + version + Constants.PackageExtension);
        }

        /// <summary>
        /// The cache path is %LocalAppData%\NuGet\Cache 
        /// </summary>
        private static string GetCachePath()
        {
            return Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "NuGet",
                                "Cache");
        }
    }
}