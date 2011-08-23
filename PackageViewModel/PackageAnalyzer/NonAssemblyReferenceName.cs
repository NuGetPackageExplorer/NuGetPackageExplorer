using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.IO;
using System.Linq;
using NuGet;
using NuGetPackageExplorer.Types;

namespace PackageExplorerViewModel.Rules {
    [Export(typeof(IPackageRule))]
    internal class NonAssemblyReferenceName : IPackageRule {

        public string Name {
            get {
                return "Non-Assembly Reference Name";
            }
        }

        public IEnumerable<PackageIssue> Check(IPackage package) {
            return from reference in package.References
                   let file = reference.File
                   where !file.EndsWith(".dll", StringComparison.OrdinalIgnoreCase) &&
                         !file.EndsWith(".exe", StringComparison.OrdinalIgnoreCase)
                   select CreateIssue(file);
        }

        private static PackageIssue CreateIssue(string reference) {
            return new PackageIssue(
                PackageIssueLevel.Warning,
                "Non-assembly reference name",
                "The name '" + reference + "' in the Filtered Assembly References is not a valid assembly name. An assembly name must have extension as either .dll or .exe.",
                "Remove this assembly reference name.");
        }
    }
}