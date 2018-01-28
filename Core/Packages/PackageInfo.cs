using System;
using NuGet.Packaging.Core;
using NuGet.Versioning;

namespace NuGetPe
{
    public class PackageInfo
    {
        public string Id { get; set; }
        public NuGetVersion SemanticVersion { get; set; }
        public string Version
        {
            get
            {
                return SemanticVersion.ToString();
            }
        }

        public PackageIdentity Identity => new PackageIdentity(Id, SemanticVersion);

        public string Authors { get; set; }
        public int DownloadCount { get; set; }
        public DateTimeOffset? Published { get; set; }

        public bool IsUnlisted
        {
            get
            {
                return Published == Constants.Unpublished;
            }
        }

        public bool IsPrerelease
        {
            get
            {
                return SemanticVersion != null && SemanticVersion.IsPrerelease;
            }
        }
        
    }
}
