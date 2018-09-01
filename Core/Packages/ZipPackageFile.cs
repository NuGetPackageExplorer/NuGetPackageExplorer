using System;
using System.IO;

namespace NuGetPe
{
    internal class ZipPackageFile : PackageFileBase
    {
        private readonly Func<Stream> _streamFactory;

        public ZipPackageFile(string entryFullName, DateTimeOffset lastWriteTime, Func<string, Stream> entryStreamFactory)
            : base(UnescapePath(entryFullName.Replace('/', '\\')))
        {
            entryFullName = UnescapePath(entryFullName);
            LastWriteTime = lastWriteTime;
            _streamFactory = () =>
            {
                try
                {
                    return entryStreamFactory(entryFullName);
                }
                catch (FileNotFoundException) // file has been renamed / moved
                {
                    entryFullName = UnescapePath(Path);

                    return entryStreamFactory(entryFullName);
                }
            };
        }

        // code copied from https://github.com/NuGet/NuGet.Client/blob/91023394890b1458ec0c5940128da73e04089869/src/NuGet.Core/NuGet.Packaging/PackageExtraction/ZipArchiveExtensions.cs#L36-L45
        private static string UnescapePath(string path)
        {
            if (path != null
                && path.IndexOf('%') > -1)
            {
                return Uri.UnescapeDataString(path);
            }

            return path;
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
