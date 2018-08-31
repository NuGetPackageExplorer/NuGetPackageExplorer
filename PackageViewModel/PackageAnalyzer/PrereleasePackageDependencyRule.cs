using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Globalization;
using System.Linq;
using NuGet.Packaging.Core;
using NuGetPackageExplorer.Types;
using NuGetPe;

namespace PackageExplorerViewModel.Rules
{
    [Export(typeof(IPackageRule))]
    internal class PrereleasePackageDependencyRule : IPackageRule
    {
        #region IPackageRule Members

        public IEnumerable<PackageIssue> Validate(IPackage package, string packagePath)
        {
            if (package.Version.IsPrerelease)
            {
                return new PackageIssue[0];
            }

            return package.DependencyGroups.SelectMany(p => p.Packages)
                                         .Where(IsPrereleaseDependency)
                                         .Select(CreatePackageIssue);
        }

        #endregion

        private static bool IsPrereleaseDependency(PackageDependency pd)
        {
            if (pd.VersionRange == null)
            {
                return false;
            }

            return pd.VersionRange.MinVersion?.IsPrerelease == true || pd.VersionRange.MaxVersion?.IsPrerelease == true;
        }

        private static PackageIssue CreatePackageIssue(PackageDependency target)
        {
            return new PackageIssue(
                PackageIssueLevel.Warning,
                "Invalid prerelease dependency",
                string.Format(CultureInfo.CurrentCulture,
                              "A stable release of a package must not have a dependency on a prerelease package, '{0}'.",
                              target),
                string.Format(CultureInfo.CurrentCulture,
                              "Either modify the version spec of dependency '{0}' or update the version field.", target)
                );
        }
    }
}
