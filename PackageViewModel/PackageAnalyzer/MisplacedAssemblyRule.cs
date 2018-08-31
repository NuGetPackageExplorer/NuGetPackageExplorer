using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Linq;
using NuGetPackageExplorer.Types;
using NuGetPe;

namespace PackageExplorerViewModel.Rules
{
    [Export(typeof(IPackageRule))]
    internal class MisplacedAssemblyRule : IPackageRule
    {
        private static readonly HashSet<string> assemblyFolders = new HashSet<string>(new[] { "lib", "analyzers", "build", "ref", "tools" }, StringComparer.OrdinalIgnoreCase);

        #region IPackageRule Members

        public IEnumerable<PackageIssue> Validate(IPackage package, string packagePath)
        {
            foreach (var file in package.GetFiles())
            {
                var path = file.Path;
                var segments = path.Split('\\');
                var directory = segments.First();

                // if under 'folder' directly
                if (assemblyFolders.Contains(directory))
                {
                    // file under the directory. Tools can do anything
                    if (segments.Length == 2 && FileHelper.IsAssembly(path) && !"tools".Equals(directory, StringComparison.OrdinalIgnoreCase))
                    {
                        yield return CreatePackageIssueForAssembliesUnderLib(path, directory);
                    }
                }
                else
                {
                    // when checking for assemblies outside known folders, only check .dll files.
                    // .exe files are often legitimate outside 'lib'.
                    if (path.EndsWith(".dll", StringComparison.OrdinalIgnoreCase) ||
                        path.EndsWith(".winmd", StringComparison.OrdinalIgnoreCase))
                    {
                        yield return CreatePackageIssueForAssembliesOutsideLib(path, directory);
                    }
                }

            }
        }

        #endregion

        private static PackageIssue CreatePackageIssueForAssembliesUnderLib(string target, string folder)
        {
            return new PackageIssue(
                PackageIssueLevel.Warning,
                "Assembly not inside a framework folder",
                "The assembly '" + target +
                $"' is placed directly under '{folder}' folder. It is recommended that assemblies be placed inside a framework-specific folder.",
                "Move it into a framework-specific folder. If this assembly is targeted for multiple frameworks, ignore this warning."
                );
        }

        private static PackageIssue CreatePackageIssueForAssembliesOutsideLib(string target, string folder)
        {
            return new PackageIssue(
                PackageIssueLevel.Warning,
                "Assembly outside known folders",
                "The assembly '" + target +
                $"' is not inside the '{folder}' folder and hence it won't be used when the package is installed into a project",
                "Move it into a known folder folder."
                );
        }
    }
}
