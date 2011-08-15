using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Linq;
using System.Text;
using NuGet;
using NuGetPackageExplorer.Types;

namespace PackageExplorerViewModel.Rules {

    [Export(typeof(IPackageRule))]
    internal class MissingSummaryRule : IPackageRule {

        public string Name {
            get { 
                return "Missing Summary Field"; 
            }
        }

        public IEnumerable<PackageIssue> Check(IPackage package) {
            if (package.Description.Length > 200 && String.IsNullOrEmpty(package.Summary)) {
                yield return new PackageIssue(
                    PackageIssueLevel.Warning,
                    "Consider provide Summary text",
                    "The Description text is long but the Summary text is empty. This means the Description text will be truncated in the 'Manage NuGet packages' dialog.",
                    "Provide a brief summary of the package in the Summary field.");
            }
        }
    }
}