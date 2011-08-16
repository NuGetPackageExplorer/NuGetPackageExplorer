using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using NuGetPackageExplorer.Types;
using System.ComponentModel.Composition;
using NuGet;
using System.IO;

namespace PackageExplorerViewModel.Rules {

    [Export(typeof(IPackageRule))]
    internal class MisplacedAssemblyRule : IPackageRule {
        public string Name {
            get {
                return "Misplaced Assembly File";
            }
        }

        public IEnumerable<PackageIssue> Check(IPackage package) {
            foreach (PackageFile file in package.GetFiles()) {
                string path = file.Path;
                if (path.EndsWith(".dll", StringComparison.OrdinalIgnoreCase)) {
                    string directory = Path.GetDirectoryName(path);
                    if (directory.Equals("lib", StringComparison.OrdinalIgnoreCase)) {
                        yield return CreatePackageIssueForAssembliesUnderLib(path);
                    }
                    else if (!directory.StartsWith("lib", StringComparison.OrdinalIgnoreCase)) {
                        yield return CreatePackageIssueForAssembliesOutsideLib(path);
                    }
                }
            }
        }

        private PackageIssue CreatePackageIssueForAssembliesUnderLib(string target) {
            return new PackageIssue(
                PackageIssueLevel.Warning,
                "Assembly not inside a framework folder",
                "The assembly '" + target + "' is placed directly under 'lib' folder. It is recommended that assemblies be placed inside a specific framework folder.",
                "Move it into a framework folder."
            );
        }

        private PackageIssue CreatePackageIssueForAssembliesOutsideLib(string target) {
            return new PackageIssue(
                PackageIssueLevel.Warning,
                "Assembly outside lib folder",
                "The assembly '" + target + "' is not inside the 'lib' folder and hence it won't be added as reference when the package is installed into a project",
                "Move it into 'lib' folder."
            );
        }
    }
}