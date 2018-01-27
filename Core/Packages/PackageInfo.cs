using System;
using System.Data.Services.Common;
using NuGet.Packaging.Core;
using NuGet.Protocol.Core.Types;
using NuGet.Versioning;

namespace NuGetPe
{
    public interface IPackageInfoType
    {
        bool ShowAll { get; set; }
    }

    [DataServiceKey("Id", "Version")]
    [HasStreamAttribute]
    public class PackageInfo : IPackageInfoType
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
            VersionDownloadCount = DownloadCount = (int)packageSearchMetadata.DownloadCount.GetValueOrDefault();
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
        public int VersionDownloadCount { get; set; }
        public int DownloadCount { get; set; }
        public bool ShowAll { get; set; }
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

        public int EffectiveDownloadCount
        {
            get
            {
                return ShowAll ? VersionDownloadCount : DownloadCount;
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
