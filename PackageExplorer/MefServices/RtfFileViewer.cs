using System.Collections.Generic;
using System.Windows.Controls;
using System.Windows.Documents;
using NuGetPackageExplorer.Types;
using NuGetPe;

namespace PackageExplorer
{
    [PackageContentViewerMetadata(100, ".rtf")]
    internal class RtfFileViewer : IPackageContentViewer
    {
        public object GetView(IPackageContent selectedFile, IReadOnlyList<IPackageContent> peerFiles)
        {
            DiagnosticsClient.TrackEvent("RtfFileViewer");

            using var stream = StreamUtility.MakeSeekable(selectedFile.GetStream(), true);
            // don't display file bigger than 1MB
            if (stream.Length > 1024 * 1024)
            {
                return "** This file is too big to view inline. ***";
            }

            var rtf = new RichTextBox
            {
                IsReadOnly = true,
                BorderThickness = new System.Windows.Thickness(0),
                VerticalScrollBarVisibility = ScrollBarVisibility.Auto,
                HorizontalScrollBarVisibility = ScrollBarVisibility.Auto
            };
            rtf.Document.MinPageWidth = 800;

            var range = new TextRange(rtf.Document.ContentStart, rtf.Document.ContentEnd);
            range.Load(stream, System.Windows.DataFormats.Rtf);

            return rtf;
        }
    }
}
