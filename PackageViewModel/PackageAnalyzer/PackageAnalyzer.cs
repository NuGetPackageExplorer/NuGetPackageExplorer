using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using NuGetPackageExplorer.Types;
using NuGet;

namespace PackageExplorerViewModel {
    [System.Diagnostics.CodeAnalysis.SuppressMessage(
        "Microsoft.Performance", 
        "CA1812:AvoidUninstantiatedInternalClasses", 
        Justification="This class is instantiated by MEF.")]
    [PackageCommandMetadata("Package Analyzer")]
    internal class PackageAnalyzer : IPackageCommand {
        public void Execute(IPackage package) {
        }
    }
}