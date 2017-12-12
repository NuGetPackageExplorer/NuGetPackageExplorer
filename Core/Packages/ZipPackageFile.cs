using System;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.IO.Packaging;
using NuGet.Packaging;

namespace NuGetPe
{
    internal class ZipPackageFile : PackageFileBase
    {
        private readonly Func<Stream> _streamFactory;

        public ZipPackageFile(PackageArchiveReader reader, ZipArchiveEntry entry) 
            : base(entry.FullName.Replace('/', '\\'))
        {
            Debug.Assert(reader != null, "reader should not be null");
            LastWriteTime = entry.LastWriteTime;
            _streamFactory = () => reader.GetStream(entry.FullName);
        }

        public override Stream GetStream()
        {
            return _streamFactory();
        }

        public override string ToString()
        {
            return Path;
        }
    }
}