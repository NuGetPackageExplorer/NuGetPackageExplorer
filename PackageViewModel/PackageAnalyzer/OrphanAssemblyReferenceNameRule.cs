using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.IO;
using System.Linq;
using NuGet;
using NuGetPackageExplorer.Types;

namespace PackageExplorerViewModel.Rules {
    [Export(typeof(IPackageRule))]
    internal class OrphanAssemblyReferenceNameRule : IPackageRule {

        public string Name {
            get {
                return "Orphan Assembly Reference Name";
            }
        }

        public IEnumerable<PackageIssue> Check(IPackage package) {
            if (package.References.Any()) {
                var allLibFiles = package.GetFilesInFolder("lib").Select(Path.GetFileName);
                var libFilesSet = new HashSet<string>(allLibFiles, StringComparer.OrdinalIgnoreCase);

                return from reference in package.References
                       where !libFilesSet.Contains(reference.File)
                       select CreateIssue(reference.File);
            }
            return new PackageIssue[0];
        }

        private static PackageIssue CreateIssue(string reference) {
            return new PackageIssue(
                PackageIssueLevel.Warning,
                "Assembly reference name not found.",
                "The name '" + reference + "' in the Filtered Assembly References is not found under the 'lib' folder.",
                "Either remove this assembly reference name or add a file with this name to the 'lib' folder.");
        }
    }
}