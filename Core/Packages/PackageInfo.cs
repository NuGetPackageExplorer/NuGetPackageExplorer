using System;
using NuGet.Packaging.Core;
using NuGet.Protocol.Core.Types;
using NuGet.Versioning;

namespace NuGetPe
{
    public class PackageInfo
    {
        public PackageInfo() { }

        public PackageInfo(IPackageSearchMetadata packageSearchMetadata)
        {
            Id = packageSearchMetadata.Identity.Id;
            if (packageSearchMetadata.Identity.HasVersion)
            {
                _version = packageSearchMetadata.Identity.Version.ToString();
                _semanticVersion = packageSearchMetadata.Identity.Version;
            }
            Authors = packageSearchMetadata.Authors;
            Published = packageSearchMetadata.Published;
            DownloadCount = (int)packageSearchMetadata.DownloadCount.GetValueOrDefault();
        }

        private string _version;
        private NuGetVersion _semanticVersion;

        public string Id { get; set; }
        public string Version
        {
            get
            {
                return _version;
            }
            set
            {
                _version = value;

                if (string.IsNullOrEmpty(_version))
                {
                    _semanticVersion = new NuGetVersion(0, 0, 0, 0);
                }
                else
                {
                    NuGetVersion.TryParse(_version, out _semanticVersion);
                }
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

        public NuGetVersion SemanticVersion
        {
            get
            {
                return _semanticVersion;
            }
        }
    }
}
