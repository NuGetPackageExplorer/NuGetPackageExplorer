using System.Collections.Generic;
using System.IO;
using NuGetPackageExplorer.Types;
using NuGetPe;

#if HAS_UNO || USE_WINUI
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media.Imaging;
#else
using System.Windows.Controls;
using System.Windows.Media.Imaging;
#endif

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
#if HAS_UNO || USE_WINUI
            source.SetSource(stream.AsRandomAccessStream());
#else
            source.BeginInit();
            source.CacheOption = BitmapCacheOption.OnLoad;
            source.StreamSource = stream;
            source.EndInit();
#endif

            var image = new Image
            {
                Source = source,
#if !HAS_UNO && !USE_WINUI
                Width = source.Width,
                Height = source.Height,
#endif
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
