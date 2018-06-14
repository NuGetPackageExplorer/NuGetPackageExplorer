using System.Windows.Controls;
using System.Windows.Documents;
using NuGetPackageExplorer.Types;

namespace PackageExplorer
{
    [PackageContentViewerMetadata(100, ".rtf")]
    internal class RtfFileViewer : IPackageContentViewer
    {
        public object GetView(string extension, System.IO.Stream stream)
        {
            stream = StreamUtility.MakeSeekable(stream);

            // don't display file bigger than 1MB
            if (stream.Length > 1024 * 1024)
            {
                return "** This file is too big to view inline. ***";
            }

            var rtf = new RichTextBox
            {
                IsReadOnly = true,
                BorderThickness = new System.Windows.Thickness(0)
            };
            rtf.Document.MinPageWidth = 800;

            var range = new TextRange(rtf.Document.ContentStart, rtf.Document.ContentEnd);
            range.Load(stream, System.Windows.DataFormats.Rtf);

            return rtf;
        }
    }
}
