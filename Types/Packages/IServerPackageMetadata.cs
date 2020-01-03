using System;

namespace NuGetPe
{
    public interface IServerPackageMetadata
    {
        Uri? ReportAbuseUrl { get; }
        int DownloadCount { get; }
    }
}
