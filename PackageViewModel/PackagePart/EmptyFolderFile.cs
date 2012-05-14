using System.IO;
using NuGet;

namespace PackageExplorerViewModel
{
    internal class EmptyFolderFile : IPackageFile
    {
        public EmptyFolderFile(PackageFolder parent)
        {
            Path = System.IO.Path.Combine(parent.Path, "_._");
        }

        public string Path
        {
            get;
            private set;
        }

        public string OriginalPath
        {
            get { return null; }
        }

        public Stream GetStream()
        {
            return new MemoryStream(new byte[0]);
        }
    }
}