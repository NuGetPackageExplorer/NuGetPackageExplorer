using NuGet.Packaging;

namespace NuGetPe
{
    public static class PackageExtensions
    {
        public static bool IsReleaseVersion(this IPackageMetadata packageMetadata)
        {
            if (packageMetadata is null)
                throw new System.ArgumentNullException(nameof(packageMetadata));
            return packageMetadata.Version.IsPrerelease;
        }

        public static string GetFullName(this IPackageMetadata package)
        {
            if (package is null)
                throw new System.ArgumentNullException(nameof(package));
            return package.Id + " " + package.Version;
        }
    }
}
