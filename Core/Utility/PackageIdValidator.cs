using System;
using System.Globalization;
using System.Text.RegularExpressions;
using NuGet.Resources;

namespace NuGet {
    public static class PackageIdValidator {
        private static readonly Regex _idRegex = new Regex(@"^\w+([_.-]\w+)*$", RegexOptions.IgnoreCase);

        public static bool IsValidPackageId(string packageId) {
            if (packageId == null) {
                throw new ArgumentNullException("packageId");
            }
            return _idRegex.IsMatch(packageId);
        }

        public static void ValidatePackageId(string packageId) {
            if (!IsValidPackageId(packageId)) {
                throw new ArgumentException(String.Format(CultureInfo.CurrentCulture, NuGetResources.InvalidPackageId, packageId));
            }
        }
    }
}
