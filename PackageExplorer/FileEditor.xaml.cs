using System;
using System.Collections.Generic;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using ICSharpCode.AvalonEdit.Highlighting;
using NuGetPackageExplorer.Types;
using PackageExplorerViewModel;
using PackageExplorer.Properties;

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
            FontChoice.SelectedItem = ConsolasFont;

            // disable unnecessary editor features
            Editor.Options.CutCopyWholeLine = false;
            Editor.Options.EnableEmailHyperlinks = false;
            Editor.Options.EnableHyperlinks = false;

            // set initial font size to check the appropriate Font Size menu item
            Settings settings = Properties.Settings.Default;
            SetFontSize(settings.FontSize);
        }

        private void UserControl_DataContextChanged(object sender, DependencyPropertyChangedEventArgs e) {
            var viewModel = e.NewValue as FileEditorViewModel;
            if (viewModel != null && viewModel.FileInEdit != null) {
                SyntaxDefinitions.SelectedItem = DeduceHighligtingDefinition(viewModel.FileInEdit.Path);
                Editor.Load(viewModel.FileInEdit.GetStream());
            }
        }

        private static IHighlightingDefinition DeduceHighligtingDefinition(string name) {
            string extension = Path.GetExtension(name).ToUpperInvariant();

            // if the extension is .pp or .transform, it is NuGet transform files.
            // in which case, we strip out this extension and examine the real extension instead
            if (extension == ".PP" || extension == ".TRANSFORM") {
                name = Path.GetFileNameWithoutExtension(name);
                extension = Path.GetExtension(name).ToUpperInvariant();
            }

            return HighlightingManager.Instance.GetDefinitionByExtension(extension) ??
                TextHighlightingDefinition.Instance;
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
            SetFontSize(size);
        }

        private void SetFontSize(int size) {
            if (size <= 8 || size >= 50) {
                size = 12;
            }
            Properties.Settings.Default.FontSize = size;

            // check the corresponding font size menu item 
            foreach (MenuItem child in FontSizeMenuItem.Items) {
                int value = Convert.ToInt32(child.Tag);
                child.IsChecked = value == size;
            }
        }
    }
}