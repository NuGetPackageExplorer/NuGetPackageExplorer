using System;

using NuGet.Packaging;

namespace NuGetPe
{
    public static class NuGetPePackageExtensions
    {
        public static bool IsReleaseVersion(this IPackageMetadata packageMetadata)
        {
            ArgumentNullException.ThrowIfNull(packageMetadata);
            return packageMetadata.Version.IsPrerelease;
        }

        public static string GetFullName(this IPackageMetadata package)
        {
            ArgumentNullException.ThrowIfNull(package);
            return package.Id + " " + package.Version;
        }
    }
}
