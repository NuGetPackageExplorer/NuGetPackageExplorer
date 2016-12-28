using System.Linq;

namespace NuGetPe
{
    public interface IPackageRepository
    {
        string Source { get; }
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Design", "CA1024:UsePropertiesWhereAppropriate")]
        IQueryable<IPackage> GetPackages();
        IQueryable<IPackage> GetPackagesById(string id, bool includePrerelease);
    }
}