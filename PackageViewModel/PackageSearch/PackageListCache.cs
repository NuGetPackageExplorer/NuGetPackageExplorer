using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using NuGet.Protocol.Core.Types;

namespace PackageExplorerViewModel.PackageSearch
{
    internal class PackageListCache<T>  where T : IPackageSearchMetadata
    {
        private readonly Dictionary<string, List<T>> _packagesDict = new Dictionary<string, List<T>>(StringComparer.OrdinalIgnoreCase);

        public void SetPackages(string packageSource, List<T> packages)
        {
            _packagesDict[packageSource] = packages;
        }

        public bool TryGetPackages(string packageSource, [NotNullWhen(true)] out List<T>? packages)
        {
            return _packagesDict.TryGetValue(packageSource, out packages);
        }
    }
}
