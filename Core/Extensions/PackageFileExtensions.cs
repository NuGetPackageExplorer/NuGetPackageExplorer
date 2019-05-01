using NuGet.Packaging;

namespace NuGetPe
{
    public static class PackageFileExtensions
    {
        public static string? OriginalPath(this IPackageFile? packageFile)
        {
            return (packageFile as PackageFileBase)?.OriginalPath;
        }
    }
}
