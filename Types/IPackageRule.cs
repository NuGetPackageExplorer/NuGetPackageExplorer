using System.Collections.Generic;
using NuGet;

namespace NuGetPackageExplorer.Types {
    public interface IPackageRule {
        string Name { get; }
        IEnumerable<PackageIssue> Check(IPackage package);
    }
}