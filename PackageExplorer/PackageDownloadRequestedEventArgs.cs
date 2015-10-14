using System;
using NuGetPe;

namespace PackageExplorer
{
    public class PackageDownloadRequestedEventArgs : EventArgs
    {
        public PackageInfo PackageInfo { get; private set; }

        public PackageDownloadRequestedEventArgs(PackageInfo packageInfo)
        {
            PackageInfo = packageInfo;
        }
    }
}
