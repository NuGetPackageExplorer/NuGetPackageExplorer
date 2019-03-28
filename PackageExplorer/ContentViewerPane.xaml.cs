using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using ICSharpCode.AvalonEdit.Highlighting;
using ICSharpCode.AvalonEdit.Search;
using NuGetPackageExplorer.Types;
using NuGetPe;

namespace PackageExplorer
{
    /// <summary>
    /// Interaction logic for ContentViewerPane.xaml
    /// </summary>
    public partial class ContentViewerPane : UserControl
    {
        private readonly CommandBinding _findCommand;

#pragma warning disable CS8618 // Non-nullable field is uninitialized.
        public ContentViewerPane()
#pragma warning restore CS8618 // Non-nullable field is uninitialized.
        {
            InitializeComponent();

            SyntaxHighlightingHelper.RegisterHightingExtensions();

            // set the Syntax Highlighting definitions
            LanguageBox.ItemsSource = HighlightingManager.Instance.HighlightingDefinitions;

            // disable unnecessary editor features
            contentBox.Options.CutCopyWholeLine = false;
            contentBox.Options.EnableEmailHyperlinks = false;
            contentBox.Options.EnableHyperlinks = false;
            contentBox.TextArea.SelectionCornerRadius = 0;

#pragma warning disable CS0618 // Type or member is obsolete
            var searchInput = new SearchInputHandler(contentBox.TextArea);
#pragma warning restore CS0618 // Type or member is obsolete
            _findCommand = searchInput.CommandBindings.FirstOrDefault(binding => binding.Command == ApplicationCommands.Find);
            contentBox.TextArea.DefaultInputHandler.NestedInputHandlers.Add(searchInput);
        }

        private void UserControl_DataContextChanged(object sender, DependencyPropertyChangedEventArgs e)
        {
            var info = (FileContentInfo)DataContext;
            if (info != null && info.IsTextFile)
            {
                DiagnosticsClient.TrackEvent("ContentViewer_LoadTextFile");
                LanguageBox.SelectedItem = SyntaxHighlightingHelper.GuessHighligtingDefinition(info.File.Name);
                contentBox.ScrollToHome();
                contentBox.Load(StreamUtility.ToStream((string)info.Content));
            }
            else
            {
                contentBox.Clear();
            }
        }

        private void UserControl_Loaded(object sender, RoutedEventArgs e)
        {
            var rootWindow = Window.GetWindow(this);
            if (rootWindow != null && _findCommand != null)
            {
                // add the Find command to the window so that we can press Ctrl+F from anywhere to bring up the search box
                rootWindow.CommandBindings.Add(_findCommand);
            }
        }
    }
}
