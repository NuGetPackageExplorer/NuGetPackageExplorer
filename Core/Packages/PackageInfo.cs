using System;
using System.Data.Services.Common;

namespace NuGet
{
    [DataServiceKey("Id", "Version")]
    [HasStreamAttribute]
    public class PackageInfo
    {
        public string Id { get; set; }
        public string Version { get; set; }
        public string Authors { get; set; }
        public int VersionDownloadCount { get; set; }
        public int DownloadCount { get; set; }
        public string PackageHash { get; set; }
        public Uri DownloadUrl { get; set; }
        public DateTimeOffset LastUpdated { get; set; }
        public long PackageSize { get; set; }

        public int EffectiveDownloadCount
        {
            get { return Math.Max(DownloadCount, VersionDownloadCount); }
        }

        public DataServicePackage AsDataServicePackage()
        {
            return new DataServicePackage
                   {
                       Id = Id,
                       Version = Version,
                       Authors = Authors,
                       VersionDownloadCount = VersionDownloadCount,
                       DownloadCount = DownloadCount,
                       PackageHash = PackageHash,
                       LastUpdated = LastUpdated
                   };
        }
    }
}