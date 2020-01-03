using System.Collections.Generic;
using System.IO;
using System.Windows.Controls;
using System.Windows.Media.Imaging;
using NuGetPackageExplorer.Types;
using NuGetPe;

namespace PackageExplorer
{
    [PackageContentViewerMetadata(99, ".jpg", ".gif", ".png", ".tif", ".bmp", ".ico")]
    internal class ImageFileViewer : IPackageContentViewer
    {
        public object GetView(IPackageContent selectedFile, IReadOnlyList<IPackageContent> peerFiles)
        {
            DiagnosticsClient.TrackEvent("ImageFileViewer");

            using var stream = StreamUtility.MakeSeekable(selectedFile.GetStream(), true);
            var source = new BitmapImage();
            source.BeginInit();
            source.CacheOption = BitmapCacheOption.OnLoad;
            source.StreamSource = stream;
            source.EndInit();

            var image = new Image
            {
                Source = source,
                Width = source.Width,
                Height = source.Height,

            };

            return new ScrollViewer
            {
                HorizontalScrollBarVisibility = ScrollBarVisibility.Auto,
                VerticalScrollBarVisibility = ScrollBarVisibility.Auto,
                Content = image
            };
        }
    }
}
