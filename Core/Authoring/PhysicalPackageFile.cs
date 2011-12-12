using System.IO;

namespace NuGet
{
    public sealed class PhysicalPackageFile : IPackageFile
    {
        private readonly bool _isTempFile;

        public PhysicalPackageFile(bool isTempFile)
        {
            _isTempFile = isTempFile;
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
        public string SourcePath { get; set; }

        /// <summary>
        /// Path in package
        /// </summary>
        public string TargetPath { get; set; }

        #region IPackageFile Members

        string IPackageFile.Path
        {
            get { return TargetPath; }
        }

        public Stream GetStream()
        {
            return File.OpenRead(SourcePath);
        }

        #endregion

        public override string ToString()
        {
            return TargetPath;
        }
    }
}