using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.IO;
using System.Linq;
using NuGetPackageExplorer.Types;
using NuGetPe;

namespace PackageExplorerViewModel.Rules
{
    [Export(typeof(IPackageRule))]
    internal class MisnamedNativeBuildFileRule : IPackageRule
    {
        #region IPackageRule Members

        public IEnumerable<PackageIssue> Validate(IPackage package, string packagePath)
        {
            var files =
                package.GetFiles().Where(x =>
                    x.Path.EndsWith(".props", StringComparison.OrdinalIgnoreCase) ||
                    x.Path.EndsWith(".targets", StringComparison.OrdinalIgnoreCase));

            foreach (var file in files)
            {
                var path = file.Path;
                var segments = path.Split('\\');

                var frameworkFolder = segments[^2];
                var filename = segments.Last();
                var filenameWithoutExtension = Path.GetFileNameWithoutExtension(filename);

                if (string.Equals(frameworkFolder, "native", StringComparison.OrdinalIgnoreCase) &&
                    filenameWithoutExtension != package.Id)
                {
                    yield return CreatePackageIssueForMisnamedNativeBuildFile(filename, package.Id);
                }
            }
        }

        #endregion


        private static PackageIssue CreatePackageIssueForMisnamedNativeBuildFile(string filename, string packageName)
        {
            return new PackageIssue(
                PackageIssueLevel.Warning,
                "Native build file misnamed",
                $"The build file '{filename}' does not match the NuGet package name. For native packages, this will cause incorrect behavior when being referenced.",
                $"Rename the build file '{filename}' to match the NuGet package name '{packageName}'."
            );
        }
    }
}
