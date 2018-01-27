using NuGet.Common;
using NuGet.Packaging;
using System;

namespace NuGetPe
{
    public static class PackageExtensions
    {
        public static bool IsReleaseVersion(this IPackageMetadata packageMetadata)
        {
            return packageMetadata.Version.IsPrerelease;
        }

        public static string GetHash(this IPackage package)
        {
            return GetHash(package, new CryptoHashProvider());
        }

        public static string GetHash(this IPackage package, CryptoHashProvider hashProvider)
        {
            using (var stream = package.GetStream())
                return Convert.ToBase64String(hashProvider.CalculateHash(stream));
        }

        public static string GetFullName(this IPackageMetadata package)
        {
            return package.Id + " " + package.Version;
        }

        public static bool IsListed(this IPackage package)
        {
            return package.Published > Constants.Unpublished;
        }
    }
}
