using System;
using System.Data.Services.Common;
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

        public string Authors { get; set; }
        public int VersionDownloadCount { get; set; }
        public int DownloadCount { get; set; }
        public string PackageHash { get; set; }
        public Uri DownloadUrl { get; set; }
        public long PackageSize { get; set; }
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

        public bool IsLocalPackage
        {
            get
            {
                return DownloadUrl.IsFile;
            }
        }

        public NuGetVersion SemanticVersion
        {
            get
            {
                return _semanticVersion;
            }
        }

        public DataServicePackage AsDataServicePackage()
        {
            return new DataServicePackage
                   {
                       Id = Id,
                       Version = _semanticVersion.ToNormalizedString(),
                       Authors = Authors,
                       VersionDownloadCount = VersionDownloadCount,
                       DownloadCount = DownloadCount,
                       PackageHash = PackageHash,
                       Published = Published,
                       IsPrerelease = IsPrerelease
                   };
        }
    }
}