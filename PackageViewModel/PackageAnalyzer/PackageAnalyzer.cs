using NuGet;
using NuGetPackageExplorer.Types;

namespace PackageExplorerViewModel {
    [System.Diagnostics.CodeAnalysis.SuppressMessage(
        "Microsoft.Performance", 
        "CA1812:AvoidUninstantiatedInternalClasses", 
        Justification="This class is instantiated by MEF.")]
    [PackageCommandMetadata("Analyze Package")]
    internal class PackageAnalyzer : IPackageCommand {
        public void Execute(IPackage package) {
        }
    }
}