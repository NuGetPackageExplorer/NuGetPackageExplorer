using System;
using System.Diagnostics;
using System.IO;
using System.IO.Packaging;

namespace NuGet
{
    internal class ZipPackageFile : IPackageFile
    {
        private readonly string _path;
        private readonly Func<MemoryStream> _streamFactory;

        public ZipPackageFile(PackagePart part)
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
            _path = UriUtility.GetPath(part.Uri);
            _streamFactory = () => new MemoryStream(buffer);
        }

        #region IPackageFile Members

        public string OriginalPath
        {
            get { return null; }
        }

        public string Path
        {
            get { return _path; }
        }

        public Stream GetStream()
        {
            return _streamFactory();
        }

        #endregion

        public override string ToString()
        {
            return Path;
        }
    }
}