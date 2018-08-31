using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.IO;
using NuGetPackageExplorer.Types;
using NuGetPe;

namespace PackageExplorerViewModel.Rules
{
    [Export(typeof(IPackageRule))]
    internal class MisplacedScriptFileRule : IPackageRule
    {
        private const string ToolsFolder = "tools";
        private const string ScriptExtension = ".ps1";

        #region IPackageRule Members

        public IEnumerable<PackageIssue> Validate(IPackage package, string packagePath)
        {
            foreach (var file in package.GetFiles())
            {
                var path = file.Path;
                if (!path.EndsWith(ScriptExtension, StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }

                if (!path.StartsWith(ToolsFolder + Path.DirectorySeparatorChar, StringComparison.OrdinalIgnoreCase))
                {
                    yield return CreatePackageIssueForMisplacedScript(path);
                }
                else
                {
                    var directory = Path.GetDirectoryName(path);
                    var name = Path.GetFileNameWithoutExtension(path);
                    if (!directory.Equals(ToolsFolder, StringComparison.OrdinalIgnoreCase) ||
                        !name.Equals("install", StringComparison.OrdinalIgnoreCase) &&
                        !name.Equals("uninstall", StringComparison.OrdinalIgnoreCase) &&
                        !name.Equals("init", StringComparison.OrdinalIgnoreCase))
                    {
                        yield return CreatePackageIssueForUnrecognizedScripts(path);
                    }
                }
            }
        }

        #endregion

        private static PackageIssue CreatePackageIssueForMisplacedScript(string path)
        {
            return new PackageIssue(
                PackageIssueLevel.Warning,
                "PowerScript file outside tools folder",
                "The script file '" + path +
                "' is outside the 'tools' folder and hence will not be executed during installation of this package.",
                "Move it into the 'tools' folder.");
        }

        private static PackageIssue CreatePackageIssueForUnrecognizedScripts(string path)
        {
            return new PackageIssue(
                PackageIssueLevel.Warning,
                "Unrecognized PowerScript file",
                "The script file '" + path +
                "' is not recognized by NuGet and hence will not be executed during installation of this package.",
                "Rename it to install.ps1, uninstall.ps1 or init.ps1 and place it directly under 'tools'.");
        }
    }
}