using System.IO;

namespace NuGet {
    public sealed class PhysicalPackageFile : IPackageFile {        
        /// <summary>
        /// Path on disk
        /// </summary>
        public string SourcePath {
            get;
            set;
        }

        /// <summary>
        /// Path in package
        /// </summary>
        public string TargetPath {
            get;
            set;
        }

        string IPackageFile.Path {
            get {
                return TargetPath;
            }
        }

        public Stream GetStream() {
            return File.OpenRead(SourcePath);
        }

        public override string ToString() {
            return TargetPath;
        }
    }
}
