using System;
using System.Diagnostics;
using System.IO;
using System.IO.Packaging;
using NuGet.Packaging;

namespace NuGetPe
{
    internal class ZipPackageFile : PackageFileBase
    {
        private readonly Func<Stream> _streamFactory;

        public ZipPackageFile(PackageArchiveReader reader, string path) 
            : base(path.Replace('/', '\\'))
        {
            Debug.Assert(reader != null, "part should not be null");
            _streamFactory = () => reader.GetStream(path);
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