using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;

namespace NuGetPe
{
    public sealed class PackageInfoEqualityComparer : IEqualityComparer<PackageInfo>
    {
        [SuppressMessage("Microsoft.Security", "CA2104:DoNotDeclareReadOnlyMutableReferenceTypes")] public static readonly PackageInfoEqualityComparer Instance = new PackageInfoEqualityComparer();

        private PackageInfoEqualityComparer()
        {
        }

        #region IEqualityComparer<PackageInfo> Members

        public bool Equals(PackageInfo x, PackageInfo y)
        {
            if (x == y)
            {
                return true;
            }

            if (x == null || y == null)
            {
                return false;
            }

            return x.Id.Equals(y.Id, StringComparison.OrdinalIgnoreCase);
        }

        public int GetHashCode(PackageInfo obj)
        {
            return obj.Id.GetHashCode();
        }

        #endregion
    }
}