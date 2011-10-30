using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Globalization;
using NuGet;
using NuGetPackageExplorer.Types;

namespace PackageExplorerViewModel.Rules {
    [Export(typeof(IPackageRule))]
    internal class PrereleasePackageDependencyRule : IPackageRule {
        public IEnumerable<PackageIssue> Validate(IPackage package, string packagePath)
        {
            if (package.IsReleaseVersion()) {
                foreach (var dependency in package.Dependencies) {
                    if (IsPrereleaseDependency(dependency)) {
                        yield return CreatePackageIssue(dependency);
                    }
                }
            }
        }

        private static bool IsPrereleaseDependency(PackageDependency dependency) {
            var versionSpec = dependency.VersionSpec;
            if (versionSpec != null) {
                return (versionSpec.MinVersion != null && !String.IsNullOrEmpty(dependency.VersionSpec.MinVersion.SpecialVersion)) ||
                       (versionSpec.MaxVersion != null && !String.IsNullOrEmpty(dependency.VersionSpec.MaxVersion.SpecialVersion));
            }
            return false;
        }

        private static PackageIssue CreatePackageIssue(PackageDependency target) {
            return new PackageIssue(
                PackageIssueLevel.Error,
                "Invalid prerelease dependency",
                String.Format(CultureInfo.CurrentCulture, "A stable release of a package must not have a dependency on a prerelease package, '{0}'.", target.ToString()),
                String.Format(CultureInfo.CurrentCulture, "Either modify the version spec of dependency '{0}' or update the version field.", target.ToString())
            );
        }
    }
}