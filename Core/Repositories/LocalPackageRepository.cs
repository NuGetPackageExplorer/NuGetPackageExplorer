using System;
using System.IO;
using System.Linq;

namespace NuGet
{
    public class LocalPackageRepository : IPackageRepository
    {
        private readonly string _source;

        public LocalPackageRepository(string source)
        {
            if (String.IsNullOrEmpty(source))
            {
                throw new ArgumentException("Argument cannot be null or empty.", "source");
            }

            _source = source;
        }

        public bool SupportsPrereleasePackages
        {
            get
            {
                return true;
            }
        }

        public string Source
        {
            get { return _source; }
        }

        public IQueryable<IPackage> GetPackages()
        {
            if (!Directory.Exists(Source))
            {
                throw new InvalidOperationException("The source directory at '" + Source + "' does not exist.");
            }

            return (from file in Directory.EnumerateFiles(Source, "*" + Constants.PackageExtension, SearchOption.TopDirectoryOnly)
                    select new ZipPackage(file)).AsQueryable();
        }
    }
}
