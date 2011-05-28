using System.IO;
using System.Windows.Controls;
using System.Windows.Media.Imaging;
using NuGetPackageExplorer.Types;

namespace PackageExplorer {
    [PackageContentViewerMetadata(99, ".jpg", ".gif", ".png")]
    internal class ImageFileViewer : IPackageContentViewer {
        public object GetView(string extension, Stream stream) {
            var source = new BitmapImage();
            source.BeginInit();
            source.CacheOption = BitmapCacheOption.OnLoad;
            source.StreamSource = stream;
            source.EndInit();

            return new Image {
                Source = source,
                Width = source.Width,
                Height = source.Height
            };
        }
    }
}