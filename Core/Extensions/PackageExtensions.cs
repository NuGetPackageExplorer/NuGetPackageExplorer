using NuGet.Packaging;

namespace NuGetPe
{
    public static class PackageExtensions
    {
        public static bool IsReleaseVersion(this IPackageMetadata packageMetadata)
        {
            System.ArgumentNullException.ThrowIfNull(packageMetadata);
            return packageMetadata.Version.IsPrerelease;
        }

        public static string GetFullName(this IPackageMetadata package)
        {
            System.ArgumentNullException.ThrowIfNull(package);
            return package.Id + " " + package.Version;
        }
    }
}
