using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.IO;
using System.Linq;
using NuGet;
using NuGetPackageExplorer.Types;

namespace PackageExplorerViewModel.Rules {
    [Export(typeof(IPackageRule))]
    internal class NonAssemblyInsideLibRule : IPackageRule {
        public IEnumerable<PackageIssue> Validate(IPackage package, string packagePath)
        {
            var allLibFiles = package.GetFilesInFolder("lib");
            var assembliesSet = new HashSet<string>(allLibFiles.Where(FileHelper.IsAssembly), StringComparer.OrdinalIgnoreCase);

            return from path in allLibFiles
                   where !FileHelper.IsAssembly(path) && !IsMatchingPdbOrXml(path, assembliesSet)
                   select CreatePackageIssue(path);
        }

        private static bool IsMatchingPdbOrXml(string path, HashSet<string> assemblies) {
            if (path.EndsWith(".xml", StringComparison.OrdinalIgnoreCase) ||
                path.EndsWith(".pdb", StringComparison.OrdinalIgnoreCase)) {

                return assemblies.Contains(Path.ChangeExtension(path, ".dll")) ||
                       assemblies.Contains(Path.ChangeExtension(path, ".winmd")) ||
                       assemblies.Contains(Path.ChangeExtension(path, ".exe"));
            }

            return false;
        }

        private static PackageIssue CreatePackageIssue(string target) {
            return new PackageIssue(
                PackageIssueLevel.Warning,
                "Incompatible files in lib folder",
                "The file '" + target + "' is not a valid assembly. If it is a XML documentation file or a .pdb file, there is no matching assembly specified in the same folder.",
                "Either remove this file from 'lib' folder or add a matching assembly for it."
            );
        }
    }
}