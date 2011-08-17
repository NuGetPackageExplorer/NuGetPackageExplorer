using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.IO;
using NuGet;
using NuGetPackageExplorer.Types;

namespace PackageExplorerViewModel.Rules {
    
    [Export(typeof(IPackageRule))]
    internal class MisplacedTransformFileRule : IPackageRule {
        private const string ContentFolder = "content";
        private const string CodeTransformExtension = ".pp";
        private const string ConfigTransformExtension = ".transform";

        public string Name {
            get {
                return "Misplaced Transform File";
            }
        }

        public IEnumerable<PackageIssue> Check(IPackage package) {
            foreach (PackageFile file in package.GetFiles()) {
                string path = file.Path;
                if (!path.EndsWith(CodeTransformExtension, StringComparison.OrdinalIgnoreCase) &&
                    !path.EndsWith(ConfigTransformExtension, StringComparison.OrdinalIgnoreCase)) {
                    continue;
                }

                string directory = Path.GetDirectoryName(path);
                if (!directory.Equals(ContentFolder, StringComparison.OrdinalIgnoreCase) &&
                    !directory.StartsWith("content\\", StringComparison.OrdinalIgnoreCase)) {
                    yield return CreatePackageIssueForMisplacedContent(path);
                }
            }
        }

        private static PackageIssue CreatePackageIssueForMisplacedContent(string path) {
            return new PackageIssue(
                PackageIssueLevel.Warning,
                "Transform file outside contents folder",
                "The transform file '" + path + "' is outside the 'contents' folder and hence will not be transformed during installation of this package.",
                "Move it into the 'contents' folder.");
        }
    }
}