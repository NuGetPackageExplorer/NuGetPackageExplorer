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

        public string Authors { get; set; }
        public int DownloadCount { get; set; }
        public DateTimeOffset? Published { get; set; }

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
