using System.Collections.Generic;
using NuGetPackageExplorer.Types;

#if HAS_UNO || USE_WINUI
using Microsoft.UI.Xaml.Controls;
#else
using System.Windows.Controls;
#endif

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
