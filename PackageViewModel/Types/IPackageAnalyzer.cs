using System.Collections.Generic;
using NuGet;

namespace PackageExplorerViewModel.Types {
    public interface IPackageAnalyzer {
        IEnumerable<PackageProblem> Analyze(IPackageMetadata packageMetadata, IEnumerable<IPackageFile> files);
    }
}