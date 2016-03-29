using System;
using System.Diagnostics;
using System.IO;
using System.IO.Packaging;
using System.Runtime.Versioning;

namespace NuGet
{
    internal class ZipPackageFile : PackageFileBase
    {
        private readonly Func<Stream> _streamFactory;

        public ZipPackageFile(PackagePart part) 
            : base(UriUtility.GetPath(part.Uri))
        {
            Debug.Assert(part != null, "part should not be null");
            _streamFactory = () => part.GetStream();
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