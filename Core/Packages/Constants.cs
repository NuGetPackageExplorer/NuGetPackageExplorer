using System;

namespace NuGetPe
{
    public static class Constants
    {
        internal const string PackageServiceEntitySetName = "Packages";

        internal const string PackageRelationshipNamespace = "http://schemas.microsoft.com/packaging/2010/07/";
        public static readonly string PackageExtension = ".nupkg";
        public static readonly string SymbolPackageExtension = ".snupkg";
        public static readonly string ManifestExtension = ".nuspec";
        public static readonly string ContentDirectory = "content";
        public static readonly string LibDirectory = "lib";
        public static readonly string ToolsDirectory = "tools";
        public static readonly string BuildDirectory = "build";

        // Starting from nuget 2.0, we use a file with the special name '_._' to represent an empty folder.
        public const string PackageEmptyFileName = "_._";

        // This is temporary until we fix the gallery to have proper first class support for this.
        // The magic unpublished date is 1900-01-01T00:00:00
        public static readonly DateTimeOffset Unpublished = new DateTimeOffset(1900, 1, 1, 0, 0, 0, TimeSpan.Zero); // v3
        public static readonly DateTimeOffset V2Unpublished = new DateTimeOffset(1900, 1, 1, 0, 0, 0, TimeZoneInfo.Local.BaseUtcOffset); // v2
    }
}
