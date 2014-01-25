using System.Linq;

namespace NuGet
{
    public interface IPackageSearchable
    {
        IQueryable<IPackage> Search(string searchTerm, bool includePrerelease);
    }
}
