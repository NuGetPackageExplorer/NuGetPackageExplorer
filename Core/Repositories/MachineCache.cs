using System;
using System.IO;
using System.Linq;

namespace NuGet {
    /// <summary>
    /// The machine cache represents a location on the machine where packages are cached. It is a specific implementation of a local repository and can be used as such.
    /// </summary>
    public class MachineCache : IPackageRepository {
        private static readonly MachineCache _default = new MachineCache();

        private readonly string _cacheRoot;

        // Disable caching since we don't want to cache packages in memory
        private MachineCache() {
            _cacheRoot = GetCachePath();
        }

        public static MachineCache Default {
            get {
                return _default;
            }
        }

        public string Source {
            get {
                return _cacheRoot;
            }
        }

        public IQueryable<IPackage> GetPackages() {
            throw new NotSupportedException();
        }

        public IPackage FindPackage(string packageId, Version version) {
            string path = GetPackageFilePath(packageId, version);

            if (File.Exists(path)) {
                return new ZipPackage(path);
            }
            else {
                return null;
            }
        }

        private string GetPackageFilePath(string id, Version version) {
            return Path.Combine(Source, id + "." + version.ToString() + Constants.PackageExtension);
        }

        /// <summary>
        /// The cache path is %LocalAppData%\NuGet\Cache 
        /// </summary>
        private static string GetCachePath() {
            return Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "NuGet", "Cache");
        }
    }
}