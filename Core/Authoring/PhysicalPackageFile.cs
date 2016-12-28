using System.IO;

namespace NuGetPe
{
    public sealed class PhysicalPackageFile : PackageFileBase
    {
        private readonly bool _isTempFile;
        private readonly string _originalPath;

        public PhysicalPackageFile(bool isTempFile, string originalPath, string targetPath)
            : base(targetPath)
        {
            _isTempFile = isTempFile;
            _originalPath = originalPath;
        }

        public bool IsTempFile
        {
            get
            {
                return _isTempFile;
            }
        }

        /// <summary>
        /// Path on disk
        /// </summary>
        public override string OriginalPath 
        {
            get
            {
                return _originalPath;
            }
        }
       
        public override Stream GetStream()
        {
            return File.OpenRead(_originalPath);
        }

        public override string ToString()
        {
            return Path;
        }
    }
}