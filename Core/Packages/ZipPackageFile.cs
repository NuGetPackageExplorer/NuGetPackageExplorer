using System;
using System.Diagnostics;
using System.IO;
using System.IO.Packaging;
using System.Runtime.Versioning;

namespace NuGet
{
    internal class ZipPackageFile : PackageFileBase
    {
        private readonly Func<MemoryStream> _streamFactory;

        public ZipPackageFile(PackagePart part) 
            : base(UriUtility.GetPath(part.Uri))
        {
            Debug.Assert(part != null, "part should not be null");

            byte[] buffer;
            using (Stream partStream = part.GetStream())
            {
                using (var stream = new MemoryStream())
                {
                    partStream.CopyTo(stream);
                    buffer = stream.ToArray();
                }
            }
            _streamFactory = () => new MemoryStream(buffer);
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