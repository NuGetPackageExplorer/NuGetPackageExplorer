using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Linq;
using NuGet;
using NuGetPackageExplorer.Types;

namespace PackageExplorerViewModel {
    [System.Diagnostics.CodeAnalysis.SuppressMessage(
        "Microsoft.Performance", 
        "CA1812:AvoidUninstantiatedInternalClasses", 
        Justification="This class is instantiated by MEF.")]
    [PackageCommandMetadata("Analyze Package")]
    internal class PackageAnalyzer : IPackageCommand {

        [ImportMany(AllowRecomposition=true)]
        public IList<IPackageRule> Rules { get; set; }

        public void Execute(IPackage package) {
            if (Rules == null) {
                return;
            }

            List<PackageIssue> allIssues = Rules.SelectMany(r => r.Check(package)).ToList();
            
            
        }
    }
}