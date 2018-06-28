using System.Collections.Generic;
using System.ComponentModel.Composition;
using NuGetPackageExplorer.Types;
using NuGetPe;

namespace PackageExplorerViewModel.Rules
{
    [Export(typeof(IPackageRule))]
    internal class MissingSummaryRule : IPackageRule
    {
        private const int DescriptionLengthThreshold = 300;

        #region IPackageRule Members

        public IEnumerable<PackageIssue> Validate(IPackage package, string packagePath)
        {
            if (package.Description.Length > DescriptionLengthThreshold && string.IsNullOrEmpty(package.Summary))
            {
                yield return new PackageIssue(
                    PackageIssueLevel.Warning,
                    "Consider providing Summary text",
                    "The Description text is long but the Summary text is empty. This means the Description text will be truncated in the 'Manage NuGet packages' dialog.",
                    "Provide a brief summary of the package in the Summary field.");
            }
        }

        #endregion
    }
}