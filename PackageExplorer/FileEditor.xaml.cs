using System;
using System.Collections.Generic;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using ICSharpCode.AvalonEdit.Highlighting;
using PackageExplorerViewModel;
using NuGetPackageExplorer.Types;

namespace PackageExplorer {
    public partial class FileEditor : UserControl, IFileEditorService {
        public FileEditor() {
            InitializeComponent();

            // TODO: figure out to add 
            var definitions = new List<IHighlightingDefinition>();
            definitions.Add(null);
            definitions.AddRange(HighlightingManager.Instance.HighlightingDefinitions);

            SyntaxDefinitions.ItemsSource = definitions;
        }

        private void UserControl_DataContextChanged(object sender, DependencyPropertyChangedEventArgs e) {
            var viewModel = e.NewValue as FileEditorViewModel;
            if (viewModel != null && viewModel.FileInEdit != null) {
                Editor.Load(viewModel.FileInEdit.GetStream());
            }
        }

        void IFileEditorService.Save(string filePath) {
            if (filePath == null) {
                throw new ArgumentNullException("filePath");
            }

            Editor.Save(filePath);
        }
    }
}