using NuGet.Packaging;

namespace NuGetPe
{
    public static class PackageExtensions
    {
        public static bool IsReleaseVersion(this IPackageMetadata packageMetadata)
        {
            return packageMetadata.Version.IsPrerelease;
        }

        public static string GetFullName(this IPackageMetadata package)
        {
            return package.Id + " " + package.Version;
        }
    }
}
