using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Linq;
using NuGet;
using NuGetPackageExplorer.Types;

namespace PackageExplorerViewModel.Rules {

    [Export(typeof(IPackageRule))]
    internal class MissingContentOrDependencyRule : IPackageRule {
        public IEnumerable<PackageIssue> Validate(IPackage package) {
            if (!HasContentOrDependency(package)) {
                yield return new PackageIssue(
                    PackageIssueLevel.Error,
                    "Package has no content or dependency.",
                    "The package does not contain any files or dependencies or framework assembly references to be a valid package.",
                    "Add files or package dependencies or framework assembly references."
                );
            }
        }

        private static bool HasContentOrDependency(IPackage package) {
            return package.GetFiles().Any() || package.Dependencies.Any() || package.FrameworkAssemblies.Any();
        }
    }
}
