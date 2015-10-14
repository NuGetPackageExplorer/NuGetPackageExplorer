using System.Linq;

namespace NuGetPe
{
    public interface IPackageSearchable
    {
        IQueryable<IPackage> Search(string searchTerm, bool includePrerelease);
    }
}
