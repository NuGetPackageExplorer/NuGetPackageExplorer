using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Globalization;
using NuGet;
using NuGetPackageExplorer.Types;

namespace PackageExplorerViewModel.Rules
{
    [Export(typeof(IPackageRule))]
    internal class PrereleasePackageDependencyRule : IPackageRule
    {
        #region IPackageRule Members

        public IEnumerable<PackageIssue> Validate(IPackage package, string packagePath)
        {
            if (package.IsReleaseVersion())
            {
                foreach (PackageDependency dependency in package.Dependencies)
                {
                    if (IsPrereleaseDependency(dependency))
                    {
                        yield return CreatePackageIssue(dependency);
                    }
                }
            }
        }

        #endregion

        private static bool IsPrereleaseDependency(PackageDependency dependency)
        {
            IVersionSpec versionSpec = dependency.VersionSpec;
            if (versionSpec != null)
            {
                return (versionSpec.MinVersion != null &&
                        !String.IsNullOrEmpty(dependency.VersionSpec.MinVersion.SpecialVersion)) ||
                       (versionSpec.MaxVersion != null &&
                        !String.IsNullOrEmpty(dependency.VersionSpec.MaxVersion.SpecialVersion));
            }
            return false;
        }

        private static PackageIssue CreatePackageIssue(PackageDependency target)
        {
            return new PackageIssue(
                PackageIssueLevel.Error,
                "Invalid prerelease dependency",
                String.Format(CultureInfo.CurrentCulture,
                              "A stable release of a package must not have a dependency on a prerelease package, '{0}'.",
                              target),
                String.Format(CultureInfo.CurrentCulture,
                              "Either modify the version spec of dependency '{0}' or update the version field.", target)
                );
        }
    }
}