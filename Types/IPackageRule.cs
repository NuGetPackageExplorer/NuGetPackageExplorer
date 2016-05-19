using System.Collections.Generic;
using NuGetPe;

namespace NuGetPackageExplorer.Types
{
    public interface IPackageRule
    {
        IEnumerable<PackageIssue> Validate(IPackage package, string packageFileName);
    }
}