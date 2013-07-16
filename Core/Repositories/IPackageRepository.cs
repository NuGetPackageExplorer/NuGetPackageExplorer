using System.Linq;

namespace NuGet
{
    public interface IPackageRepository
    {
        string Source { get; }
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Design", "CA1024:UsePropertiesWhereAppropriate")]
        IQueryable<IPackage> GetPackages();
    }
}