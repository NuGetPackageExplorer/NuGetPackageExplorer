using System.Collections.Generic;
using NuGet;

namespace NuGetPackageExplorer.Types {
    public interface IPackageRule {
        IEnumerable<PackageIssue> Validate(IPackage package);
    }
}