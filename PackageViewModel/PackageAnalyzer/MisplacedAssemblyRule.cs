using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.IO;
using NuGetPe;
using NuGetPackageExplorer.Types;

namespace PackageExplorerViewModel.Rules
{
    [Export(typeof(IPackageRule))]
    internal class MisplacedAssemblyRule : IPackageRule
    {
        private const string LibFolder = "lib";

        #region IPackageRule Members

        public IEnumerable<PackageIssue> Validate(IPackage package, string packagePath)
        {
            foreach (IPackageFile file in package.GetFiles())
            {
                string path = file.Path;
                string directory = Path.GetDirectoryName(path);

                // if under 'lib' directly
                if (directory.Equals(LibFolder, StringComparison.OrdinalIgnoreCase))
                {
                    if (FileHelper.IsAssembly(path))
                    {
                        yield return CreatePackageIssueForAssembliesUnderLib(path);
                    }
                }
                else if (
                    !directory.StartsWith(LibFolder + Path.DirectorySeparatorChar,
                                          StringComparison.OrdinalIgnoreCase))
                {
                    // when checking for assemblies outside 'lib' folder, only check .dll files.
                    // .exe files are often legitimate outside 'lib'.
                    if (path.EndsWith(".dll", StringComparison.OrdinalIgnoreCase) ||
                        path.EndsWith(".winmd", StringComparison.OrdinalIgnoreCase))
                    {
                        yield return CreatePackageIssueForAssembliesOutsideLib(path);
                    }
                }
            }
        }

        #endregion

        private static PackageIssue CreatePackageIssueForAssembliesUnderLib(string target)
        {
            return new PackageIssue(
                PackageIssueLevel.Warning,
                "Assembly not inside a framework folder",
                "The assembly '" + target +
                "' is placed directly under 'lib' folder. It is recommended that assemblies be placed inside a framework-specific folder.",
                "Move it into a framework-specific folder. If this assembly is targeted for multiple frameworks, ignore this warning."
                );
        }

        private static PackageIssue CreatePackageIssueForAssembliesOutsideLib(string target)
        {
            return new PackageIssue(
                PackageIssueLevel.Warning,
                "Assembly outside lib folder",
                "The assembly '" + target +
                "' is not inside the 'lib' folder and hence it won't be added as reference when the package is installed into a project",
                "Move it into 'lib' folder."
                );
        }
    }
}