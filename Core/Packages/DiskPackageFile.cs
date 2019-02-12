using System;
using System.IO;

namespace NuGetPe
{
    public class DiskPackageFile : PackageFileBase
    {
        private DateTimeOffset? _lastWriteTime;

        public DiskPackageFile(string path, string physicalPath)
            : base(path)
        {
            OriginalPath = physicalPath;
        }

        public override Stream GetStream()
        {
            return File.OpenRead(OriginalPath);
        }

        public override string? OriginalPath { get; }

        public override DateTimeOffset LastWriteTime
        {
            get
            {
                if (!_lastWriteTime.HasValue)
                {
                    _lastWriteTime = File.GetLastWriteTimeUtc(OriginalPath);
                }
                return _lastWriteTime.Value;
            }
        }

        public override string ToString()
        {
            return Path;
        }
    }
}
