using System;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using NuGet.Packaging;

namespace NuGetPe
{
    internal class ZipPackageFile : PackageFileBase
    {
        private readonly Func<Stream> _streamFactory;

        public ZipPackageFile(PackageArchiveReader reader, ZipArchiveEntry entry)
            : base(UnescapePath(entry.FullName.Replace('/', '\\')))
        {
            Debug.Assert(reader != null, "reader should not be null");
            LastWriteTime = entry.LastWriteTime;
            _streamFactory = () => reader.GetStream(UnescapePath(entry.FullName));
        }

        // code copied from https://github.com/NuGet/NuGet.Client/blob/91023394890b1458ec0c5940128da73e04089869/src/NuGet.Core/NuGet.Packaging/PackageExtraction/ZipArchiveExtensions.cs#L36-L45
        private static string UnescapePath(string path)
        {
            if (path != null
                && path.IndexOf('%') > -1)
            {
                return Uri.UnescapeDataString(path);
            }

            return path!;
        }

        public override Stream GetStream()
        {
            return _streamFactory();
        }

        public override DateTimeOffset LastWriteTime { get; }

        public override string ToString()
        {
            return Path;
        }
    }
}
