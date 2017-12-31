using System;
using System.IO;
using System.Linq;

namespace NuGetPe
{
    public class LocalPackageRepository : IPackageRepository
    {
        private readonly string _source;

        public LocalPackageRepository(string source)
        {
            if (string.IsNullOrEmpty(source))
            {
                throw new ArgumentException("Argument cannot be null or empty.", "source");
            }

            _source = source;
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


        public IQueryable<IPackage> GetPackagesById(string id, bool includePrerelease)
        {
            if (!Directory.Exists(Source))
            {
                throw new InvalidOperationException("The source directory at '" + Source + "' does not exist.");
            }

            var query = (from file in Directory.EnumerateFiles(Source, "*" + Constants.PackageExtension, SearchOption.TopDirectoryOnly)
                         let p = new ZipPackage(file)
                         where p.Id == id
                         select p);
            if (!includePrerelease)
            {
                query = query.Where(p => !p.IsPrerelease);
            }

            return query.AsQueryable();
        }
    }
}
