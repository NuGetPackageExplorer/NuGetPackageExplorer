using System.Collections.Generic;
using System.ComponentModel.Composition;
using NuGet;
using NuGetPackageExplorer.Types;

namespace PackageExplorerViewModel.Rules {

    [Export(typeof(IPackageRule))]
    internal class MissingContentOrDependencyRule : IPackageRule {

        public string Name {
            get {
                return "No Content Or Dependency";
            }
        }

        public IEnumerable<PackageIssue> Check(IPackage package) {
            if (!PackageHelper.IsPackageValid(package)) {
                yield return new PackageIssue(
                    PackageIssueLevel.Error,
                    "Package has no content or dependency.",
                    "The package does not contain any files or dependencies or framework assembly references to be a valid package.",
                    "Add files or package dependencies or framework assembly references."
                );
            }
        }
    }
}