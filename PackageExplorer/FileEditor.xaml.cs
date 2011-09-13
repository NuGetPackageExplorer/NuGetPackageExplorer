using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using ICSharpCode.AvalonEdit.Highlighting;
using NuGetPackageExplorer.Types;
using PackageExplorerViewModel;

namespace PackageExplorer {
    public partial class FileEditor : UserControl, IFileEditorService {

        private static readonly FontFamily ConsolasFont = new FontFamily("Consolas");

        public FileEditor() {
            InitializeComponent();

            // set the Syntax Highlighting definitions
            var definitions = new List<IHighlightingDefinition>();
            definitions.Add(TextHighlightingDefinition.Instance);
            definitions.AddRange(HighlightingManager.Instance.HighlightingDefinitions);
            SyntaxDefinitions.ItemsSource = definitions;

            // Set the initial Font Family to Consolas
            FontChoice.ItemsSource = Fonts.SystemFontFamilies.OrderBy(p => p.Source);
            FontChoice.SelectedItem = ConsolasFont;
            

            // disable unnecessary editor features
            Editor.Options.CutCopyWholeLine = false;
            Editor.Options.EnableEmailHyperlinks = false;
            Editor.Options.EnableHyperlinks = false;
        }

        private void UserControl_DataContextChanged(object sender, DependencyPropertyChangedEventArgs e) {
            var viewModel = e.NewValue as FileEditorViewModel;
            if (viewModel != null && viewModel.FileInEdit != null) {
                SyntaxDefinitions.SelectedItem = FileUtility.DeduceHighligtingDefinition(viewModel.FileInEdit.Path);
                Editor.Load(viewModel.FileInEdit.GetStream());
            }
        }

        void IFileEditorService.Save(string filePath) {
            if (filePath == null) {
                throw new ArgumentNullException("filePath");
            }
            Editor.Save(filePath);
        }

        private void OnFontSizeItem_Click(object sender, RoutedEventArgs e) {
            var item = (MenuItem)sender;
            int size = Convert.ToInt32(item.Tag);
            Properties.Settings.Default.FontSize = size;
        }

        private void UserControl_Loaded(object sender, RoutedEventArgs e) {
            Editor.Focus();
        }
    }
}