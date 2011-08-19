using System.Collections.Generic;
using NuGetPackageExplorer.Types;

namespace PackageExplorerViewModel {
    internal class PackageIssueComparer : IComparer<PackageIssue> {
        public static readonly PackageIssueComparer Instance = new PackageIssueComparer();

        private PackageIssueComparer() {
        }

        public int Compare(PackageIssue x, PackageIssue y) {
            if (x == null || y == null) {
                return 0;
            }

            if (x == null) {
                return -1;
            }

            if (y == null) {
                return 1;
            }

            if (x.Level != y.Level) {
                return (int)x.Level.CompareTo(y.Level);
            }

            return string.Compare(x.Title, y.Title, System.StringComparison.CurrentCultureIgnoreCase);
        }
    }
}
