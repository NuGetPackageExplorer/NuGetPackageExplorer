using System;
using System.Globalization;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using ICSharpCode.AvalonEdit.Highlighting;
using ICSharpCode.AvalonEdit.Search;
using NuGetPackageExplorer.Types;
using NuGetPe;
using PackageExplorerViewModel;

namespace PackageExplorer
{
    public partial class FileEditor : UserControl, IFileEditorService
    {
        private static readonly FontFamily ConsolasFont = new FontFamily("Consolas");

        private readonly ISettingsManager _settings;
        private readonly IUIServices _uIServices;

#pragma warning disable CS8618 // Non-nullable field is uninitialized.
        public FileEditor(ISettingsManager settings, IUIServices uIServices)
#pragma warning restore CS8618 // Non-nullable field is uninitialized.
        {
            InitializeComponent();

            _settings = settings;
            _uIServices = uIServices;
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
            DiagnosticsClient.TrackEvent("FileEditor_Save");

            if (filePath == null)
            {
                throw new ArgumentNullException("filePath");
            }

            try
            {
                Editor.Save(filePath);
            }
            catch (Exception ex)
            {
                _uIServices.Show(ex.Message, MessageLevel.Error);
            }

        }

        #endregion

        private void UserControl_DataContextChanged(object sender, DependencyPropertyChangedEventArgs e)
        {
            if (e.NewValue is FileEditorViewModel viewModel && viewModel.FileInEdit != null)
            {
                DiagnosticsClient.TrackEvent("FileEditor_Load");

                SyntaxDefinitions.SelectedItem = SyntaxHighlightingHelper.GuessHighligtingDefinition(viewModel.FileInEdit.Path);
                try
                {
                    var stream = viewModel.FileInEdit.GetStream();
                    stream = StreamUtility.MakeSeekable(stream);
                    Editor.Load(stream);
                }
                catch (Exception ex)
                {
                    _uIServices.Show(ex.Message, MessageLevel.Error);
                }

            }
        }

        private void OnFontSizeItem_Click(object sender, RoutedEventArgs e)
        {
            var item = (MenuItem)sender;
            var size = Convert.ToInt32(item.Tag, CultureInfo.InvariantCulture);
            _settings.FontSize = size;
        }

        private void UserControl_Loaded(object sender, RoutedEventArgs e)
        {
            Editor.Focus();
        }
    }
}
