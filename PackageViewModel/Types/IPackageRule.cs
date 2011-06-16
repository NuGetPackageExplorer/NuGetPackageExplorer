using System.Collections.Generic;
using NuGet;

namespace PackageExplorerViewModel.Types {
    public interface IPackageRule {
        string Name { get; }
        IEnumerable<PackageProblem> Check(IPackageMetadata packageMetadata, IEnumerable<IPackageFile> files);
    }
}