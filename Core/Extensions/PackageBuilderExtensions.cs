using NuGet.Packaging;

namespace NuGetPe
{
    public static class PackageBuilderExtensions
    {
        public static IPackage Build(this PackageBuilder builder)
        {
            return new SimplePackage(builder);
        }
    }
}
