using System;

namespace NuGet
{
    public static class Constants
    {
        internal const string PackageServiceEntitySetName = "Packages";

        internal const string PackageRelationshipNamespace = "http://schemas.microsoft.com/packaging/2010/07/";
        public static readonly string PackageExtension = ".nupkg";
        public static readonly string ManifestExtension = ".nuspec";
        public static readonly string ContentDirectory = "content";

        // This is temporary until we fix the gallery to have proper first class support for this.
        // The magic unpublished date is 1900-01-01T00:00:00
        public static readonly DateTimeOffset Unpublished = new DateTimeOffset(1900, 1, 1, 0, 0, 0, TimeSpan.FromHours(-8));
    }
}