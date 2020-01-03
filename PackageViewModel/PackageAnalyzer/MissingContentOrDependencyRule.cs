using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Linq;
using NuGetPackageExplorer.Types;
using NuGetPe;

namespace PackageExplorerViewModel.Rules
{
    [Export(typeof(IPackageRule))]
    internal class MissingContentOrDependencyRule : IPackageRule
    {
        #region IPackageRule Members

        public IEnumerable<PackageIssue> Validate(IPackage package, string packagePath)
        {
            if (!HasContentOrDependency(package))
            {
                yield return new PackageIssue(
                    PackageIssueLevel.Error,
                    "Package has no content or dependency.",
                    "The package does not contain any files or dependencies or framework assembly references to be a valid package.",
                    "Add files or package dependencies or framework assembly references."
                    );
            }
        }

        #endregion

        private static bool HasContentOrDependency(IPackage package)
        {
            return package.GetFiles().Any() ||
                   package.DependencyGroups.SelectMany(p => p.Packages).Any() ||
                   package.FrameworkReferences.Any();
        }
    }
}