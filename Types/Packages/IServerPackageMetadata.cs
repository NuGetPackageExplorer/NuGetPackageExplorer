using System;

namespace NuGetPe
{
    public interface IServerPackageMetadata
    {
        Uri? ReportAbuseUrl { get; }
        long DownloadCount { get; }
    }
}
