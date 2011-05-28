using System;
using System.Diagnostics.CodeAnalysis;
using System.Linq;

namespace NuGet {
    public interface IPackageRepository {
        string Source { get; }
        [SuppressMessage("Microsoft.Design", "CA1024:UsePropertiesWhereAppropriate", Justification = "This call might be expensive")]
        IQueryable<IPackage> GetPackages();
    }
}
