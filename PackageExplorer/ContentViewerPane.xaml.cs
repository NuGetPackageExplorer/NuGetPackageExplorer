using System.Windows.Controls;
using ICSharpCode.AvalonEdit.Highlighting;
using NuGetPackageExplorer.Types;

namespace PackageExplorer {
    /// <summary>
    /// Interaction logic for ContentViewerPane.xaml
    /// </summary>
    public partial class ContentViewerPane : UserControl {
        public ContentViewerPane() {
            InitializeComponent();

            // set the Syntax Highlighting definitions
            LanguageBox.ItemsSource = HighlightingManager.Instance.HighlightingDefinitions;

            // disable unnecessary editor features
            contentBox.Options.CutCopyWholeLine = false;
            contentBox.Options.EnableEmailHyperlinks = false;
            contentBox.Options.EnableHyperlinks = false;
        }

        private void UserControl_DataContextChanged(object sender, System.Windows.DependencyPropertyChangedEventArgs e) {
            var info = (FileContentInfo)DataContext;
            if (info != null && info.IsTextFile) {
                LanguageBox.SelectedItem = FileUtility.DeduceHighligtingDefinition(info.File.Name);
                contentBox.ScrollToHome();
                contentBox.Load(StreamUtility.ToStream((string)info.Content));
            }
            else {
                contentBox.Clear();
            }
        }
    }
}
