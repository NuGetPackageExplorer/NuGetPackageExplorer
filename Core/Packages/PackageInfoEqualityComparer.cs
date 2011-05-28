using System;
using System.Collections.Generic;

namespace NuGet {
    public sealed class PackageInfoEqualityComparer : IEqualityComparer<PackageInfo> {
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Security", "CA2104:DoNotDeclareReadOnlyMutableReferenceTypes")]
        public static readonly PackageInfoEqualityComparer Instance = new PackageInfoEqualityComparer();

        private PackageInfoEqualityComparer() {
        }

        public bool Equals(PackageInfo x, PackageInfo y) {
            if (x == y) {
                return true;
            }

            if (x == null || y == null) {
                return false;
            }

            return x.Id.Equals(y.Id, StringComparison.OrdinalIgnoreCase);
        }

        public int GetHashCode(PackageInfo obj) {
            return obj.Id.GetHashCode();
        }
    }
}
