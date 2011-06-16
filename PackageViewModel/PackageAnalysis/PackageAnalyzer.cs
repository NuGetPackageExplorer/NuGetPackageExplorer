using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Linq;
using NuGet;
using PackageExplorerViewModel.Types;

namespace PackageExplorerViewModel {

    [Export(typeof(IPackageAnalyzer))]
    internal class PackageAnalyzer : IPackageAnalyzer {

        [ImportMany]
        public IEnumerable<IPackageRule> Rules { get; set; }

        public IEnumerable<PackageProblem> Analyze(IPackageMetadata packageMetadata, IEnumerable<IPackageFile> files) {
            if (packageMetadata == null) {
                throw new ArgumentNullException("packageMetadata");
            }
            return Rules.SelectMany(rule => rule.Check(packageMetadata, files));
        }
    }
}