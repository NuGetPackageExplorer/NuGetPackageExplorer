using System;
using NuGet.Packaging.Core;
using NuGet.Versioning;

namespace NuGetPe
{
    public class PackageInfo
    {
        public PackageInfo(PackageIdentity identity)
        {
            Identity = identity ?? throw new ArgumentNullException(nameof(identity));
        }

        public PackageIdentity Identity { get; }

        public string Id => Identity.Id;
        public NuGetVersion SemanticVersion => Identity.Version;
        public string Version => SemanticVersion.ToFullString();

        public string? Description { get; set; }
        public string? Summary { get; set; }
        public string? Authors { get; set; }
        public int DownloadCount { get; set; }
        public DateTimeOffset? Published { get; set; }

        public string? IconUrl { get; set; }
        public string? LicenseUrl { get; set; }
        public string? ProjectUrl { get; set; }
        public string? Tags { get; set; }
        public string? ReportAbuseUrl { get; set; }

        public bool IsPrefixReserved { get; set; }
        public bool IsRemotePackage { get; set; }

        public bool IsUnlisted
        {
            get
            {
                return Published == Constants.Unpublished || Published == Constants.V2Unpublished;
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
