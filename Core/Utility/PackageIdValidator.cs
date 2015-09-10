using System;
using System.Globalization;
using System.Text.RegularExpressions;
using NuGet.Resources;
using NuGetPackageExplorer.Types;

namespace NuGet
{
    public static class PackageIdValidator
    {
        public const int MaxPackageIdLength = 100;
        private static readonly Regex _idRegex = new Regex(@"^\w+([_.-]\w+)*$", RegexOptions.IgnoreCase);

        public static bool IsValidPackageId(string packageId)
        {
            if (packageId == null)
            {
                throw new ArgumentNullException("packageId");
            }
            return (packageId.Length <= MaxPackageIdLength) && _idRegex.IsMatch(packageId) || ReplacementTokens.AllReplacementTokens.Contains(packageId);
        }

        public static void ValidatePackageId(string packageId)
        {
            if (packageId.Length > MaxPackageIdLength)
            {
                throw new ArgumentException(NuGetResources.Manifest_IdMaxLengthExceeded);
            }

            if (!IsValidPackageId(packageId))
            {
                throw new ArgumentException(String.Format(CultureInfo.CurrentCulture, NuGetResources.InvalidPackageId,
                                                          packageId));
            }
        }
    }
}