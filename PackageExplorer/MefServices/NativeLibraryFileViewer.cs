using System.Collections.Generic;
using System.Windows.Controls;
using NuGetPackageExplorer.Types;

namespace PackageExplorer
{
    [PackageContentViewerMetadata(100, ".so", ".dylib")]
    internal class NativeLibraryFileViewer : IPackageContentViewer
    {
        public object GetView(IPackageContent selectedFile, IReadOnlyList<IPackageContent> peerFiles)
        {
            return new Grid();
        }
    }
}
