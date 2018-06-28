using System;
using System.IO;

namespace NuGetPe
{
    public class EmptyFolderFile : PackageFileBase
    {
        public EmptyFolderFile(string folderPath)
            : base(System.IO.Path.Combine(folderPath, NuGetPe.Constants.PackageEmptyFileName))
        {
            LastWriteTime = DateTimeOffset.UtcNow;
        }

        public override Stream GetStream()
        {
            return Stream.Null;
        }

        public override DateTimeOffset LastWriteTime { get; }

    }
}
