using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.IO;
using System.Linq;
using NuGet;
using NuGetPackageExplorer.Types;

namespace PackageExplorerViewModel.Rules {

    [Export(typeof(IPackageRule))]
    internal class InvalidFrameworkFolderRule : IPackageRule {
        public IEnumerable<PackageIssue> Validate(IPackage package) {
            var set = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            foreach (var file in package.GetFiles()) {
                string path = file.Path;
                string[] parts = path.Split(Path.DirectorySeparatorChar);
                if (parts.Length >= 3 && parts[0].Equals("lib", StringComparison.OrdinalIgnoreCase)) {
                    set.Add(parts[1]);
                }
            }

            return set.Where(IsInvalidFrameworkName).Select(CreatePackageIssue);
        }

        private bool IsInvalidFrameworkName(string name) {
            return VersionUtility.ParseFrameworkName(name) == VersionUtility.UnsupportedFrameworkName;
        }

        private PackageIssue CreatePackageIssue(string target) {
            return new PackageIssue(
                PackageIssueLevel.Warning,
                "Invalid framework folder",
                "The folder '" + target + "' under 'lib' is not recognized as a valid framework name.",
                "Rename it to a valid framework name."
            );
        }
    }
}