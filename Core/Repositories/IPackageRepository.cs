using System.Linq;

namespace NuGet
{
    public interface IPackageRepository
    {
        string Source { get; }
        IQueryable<IPackage> GetPackages();
    }
}