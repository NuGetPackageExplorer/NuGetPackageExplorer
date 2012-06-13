using System.IO;
using NuGet;

namespace PackageExplorerViewModel
{
    internal class EmptyFolderFile : PackageFileBase
    {
        public EmptyFolderFile(PackageFolder parent)
            : base(System.IO.Path.Combine(parent.Path, NuGet.Constants.PackageEmptyFileName))
        {
        }

        public override Stream GetStream()
        {
            return new MemoryStream(new byte[0]);
        }
    }
}