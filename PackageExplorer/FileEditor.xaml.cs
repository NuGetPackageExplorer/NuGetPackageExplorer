using System;
using System.Globalization;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using ICSharpCode.AvalonEdit.Highlighting;
using ICSharpCode.AvalonEdit.Search;
using NuGetPackageExplorer.Types;
using PackageExplorer.Properties;
using PackageExplorerViewModel;

namespace PackageExplorer
{
    public partial class FileEditor : UserControl, IFileEditorService
    {
        private static readonly FontFamily ConsolasFont = new FontFamily("Consolas");

        public FileEditor()
        {
            InitializeComponent();

            SyntaxHighlightingHelper.RegisterHightingExtensions();

            // set the Syntax Highlighting definitions
            SyntaxDefinitions.ItemsSource = HighlightingManager.Instance.HighlightingDefinitions;

            // Set the initial Font Family to Consolas
            FontChoice.ItemsSource = Fonts.SystemFontFamilies.OrderBy(p => p.Source);
            FontChoice.SelectedItem = ConsolasFont;

            // disable unnecessary editor features
            Editor.Options.CutCopyWholeLine = false;
            Editor.Options.EnableEmailHyperlinks = false;
            Editor.Options.EnableHyperlinks = false;
            Editor.Options.ConvertTabsToSpaces = true;

            Editor.TextArea.SelectionCornerRadius = 0;

            var searchInput = SearchPanel.Install(Editor.TextArea);
        }

        #region IFileEditorService Members

        void IFileEditorService.Save(string filePath)
        {
            if (filePath == null)
            {
                throw new ArgumentNullException("filePath");
            }
            Editor.Save(filePath);
        }

        #endregion

        private void UserControl_DataContextChanged(object sender, DependencyPropertyChangedEventArgs e)
        {
            if (e.NewValue is FileEditorViewModel viewModel && viewModel.FileInEdit != null)
            {
                SyntaxDefinitions.SelectedItem = SyntaxHighlightingHelper.GuessHighligtingDefinition(viewModel.FileInEdit.Path);
                var stream = viewModel.FileInEdit.GetStream();
                stream = StreamUtility.MakeSeekable(stream);
                Editor.Load(stream);
            }
        }

        private void OnFontSizeItem_Click(object sender, RoutedEventArgs e)
        {
            var item = (MenuItem)sender;
            var size = Convert.ToInt32(item.Tag, CultureInfo.InvariantCulture);
            Settings.Default.FontSize = size;
        }

        private void UserControl_Loaded(object sender, RoutedEventArgs e)
        {
            Editor.Focus();
        }
    }
}
