using System;
using System.Collections.Generic;
using System.Globalization;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Input;
using PackageExplorerViewModel;
using NuGetPackageExplorer.Types;

namespace PackageExplorer {
    /// <summary>
    /// Interaction logic for PackageViewer.xaml
    /// </summary>
    public partial class PackageViewer : UserControl {
        private const string PackageFileDataFormat = "PackageFileContent";

        public PackageViewer(IUIServices messageBoxServices, IPackageChooser packageChooser) {
            InitializeComponent();

            PackageMetadataEditor.UIServices = messageBoxServices;
            PackageMetadataEditor.PackageChooser = packageChooser;
        }

        private void FileContentContainer_IsVisibleChanged(object sender, DependencyPropertyChangedEventArgs e) {
            var settings = Properties.Settings.Default;

            if ((bool)e.NewValue) {
                ContentGrid.RowDefinitions[0].Height = new GridLength(settings.PackageContentHeight, GridUnitType.Star);
                ContentGrid.RowDefinitions[2].Height = new GridLength(settings.ContentViewerHeight, GridUnitType.Star);
                ContentGrid.RowDefinitions[2].MinHeight = 150;

                if (FileContentContainer.Content == null) {
                    UserControl fileContent = CreateFileContentViewer();
                    FileContentContainer.Content = fileContent;
                }
            }
            else {
                settings.PackageContentHeight = ContentGrid.RowDefinitions[0].Height.Value;
                settings.ContentViewerHeight = ContentGrid.RowDefinitions[2].Height.Value;

                ContentGrid.RowDefinitions[2].Height = new GridLength(0, GridUnitType.Star);
                ContentGrid.RowDefinitions[2].MinHeight = 0;
            }
        }

        // delay load the Syntax HighlightTextBox, avoid loading SyntaxHighlighting.dll upfront
        [MethodImpl(MethodImplOptions.NoInlining)]
        private static UserControl CreateFileContentViewer() {
            var content = new ContentViewerPane();
            content.SetBinding(UserControl.DataContextProperty, new Binding("CurrentFileInfo"));
            return content;
        }

        private PackageFolder RootFolder {
            get {
                return (DataContext as PackageViewModel).RootFolder;
            }
        }

        private void OnTreeViewSelectedItemChanged(object sender, RoutedPropertyChangedEventArgs<object> e) {
            var model = DataContext as PackageViewModel;
            if (model != null) {
                model.SelectedItem = PackagesTreeView.SelectedItem;
            }
        }

        private void OnTreeViewItemDoubleClick(object sender, RoutedEventArgs args) {
            var item = (TreeViewItem)sender;
            PackageFile file = item.DataContext as PackageFile;
            if (file != null) {
                var command = ((PackageViewModel)DataContext).ViewContentCommand;
                command.Execute(file);

                args.Handled = true;
            }
        }

        private void TreeView_PreviewMouseRightButtonDown(object sender, MouseButtonEventArgs e) {
            TreeView tv = (TreeView)sender;
            IInputElement element = tv.InputHitTest(e.GetPosition(tv));
            while (!((element is TreeView) || element == null)) {
                if (element is TreeViewItem)
                    break;

                if (element is FrameworkElement) {
                    FrameworkElement fe = (FrameworkElement)element;
                    element = (IInputElement)(fe.Parent ?? fe.TemplatedParent);
                }
                else if (element is FrameworkContentElement) {
                    FrameworkContentElement fe = (FrameworkContentElement)element;
                    element = (IInputElement)fe.Parent;
                }
                else
                    break;
            }
            if (element is TreeViewItem) {
                element.Focus();
                e.Handled = true;
            }
        }

        private void OnTreeViewItemDragOver(object sender, DragEventArgs e) {
            PackageFolder folder;

            TreeViewItem item = sender as TreeViewItem;
            if (item != null) {
                folder = item.DataContext as PackageFolder;
            }
            else {
                folder = RootFolder;
            }

            DragDropEffects effects = DragDropEffects.None;
            if (folder != null) {
                var data = e.Data;
                if (data.GetDataPresent(DataFormats.FileDrop)) {
                    effects = DragDropEffects.Copy;
                }
                else {
                    PackageFile file = data.GetData(PackageFileDataFormat, false) as PackageFile;
                    // make sure we don't drag a file into the same folder
                    if (file != null &&
                        !folder.Contains(file) &&
                        !folder.ContainsFile(file.Name) &&
                        !folder.ContainsFolder(file.Name)) {
                        effects = DragDropEffects.Move;
                    }
                }
            }

            e.Effects = effects;
            e.Handled = true;
        }

        private void OnTreeViewItemDrop(object sender, DragEventArgs e) {
            PackageFolder folder = null;

            TreeViewItem item = sender as TreeViewItem;
            if (item != null) {
                folder = item.DataContext as PackageFolder;
            }

            var data = e.Data;
            if (data.GetDataPresent(DataFormats.FileDrop)) {
                object value = data.GetData(DataFormats.FileDrop);
                string[] filenames = value as string[];
                if (filenames != null && filenames.Length > 0) {
                    var viewModel = DataContext as PackageViewModel;
                    viewModel.AddDraggedAndDroppedFiles(folder, filenames);
                    e.Handled = true;
                }
            }
            else if (data.GetDataPresent(PackageFileDataFormat)) {
                PackageFile file = data.GetData(PackageFileDataFormat) as PackageFile;
                if (file != null) {
                    folder = folder ?? RootFolder;
                    folder.AddFile(file);
                    e.Handled = true;
                }
            }
        }

        private TreeViewItem _dragItem;
        private System.Windows.Point _dragPoint;
        private bool _isDragging;

        private void PackagesTreeViewItem_PreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e) {
            if (_isDragging) {
                return;
            }

            TreeViewItem item = sender as TreeViewItem;
            if (item != null) {
                // only allow dragging file
                PackageFile file = item.DataContext as PackageFile;
                if (file != null) {
                    _dragItem = item;
                    _dragPoint = e.GetPosition(item);
                    _isDragging = true;
                }
            }
        }

        private void PackagesTreeViewItem_PreviewMouseMove(object sender, MouseEventArgs e) {
            if (!_isDragging) {
                return;
            }

            TreeViewItem item = sender as TreeViewItem;
            if (item == _dragItem) {
                System.Windows.Point newPoint = e.GetPosition(item);
                if (Math.Abs(newPoint.X - _dragPoint.X) >= SystemParameters.MinimumHorizontalDragDistance ||
                    Math.Abs(newPoint.Y - _dragPoint.Y) >= SystemParameters.MinimumVerticalDragDistance) {
                    // initiate a dragging
                    PackageFile file = item.DataContext as PackageFile;
                    if (file != null) {
                        DataObject data = new DataObject(PackageFileDataFormat, file);
                        DragDrop.DoDragDrop(item, data, DragDropEffects.Move);
                        ResetDraggingState();
                    }
                }
            }
        }

        private void PackagesTreeViewItem_PreviewMouseLeftButtonUp(object sender, MouseButtonEventArgs e) {
            if (_isDragging) {
                ResetDraggingState();
            }
        }

        private void ResetDraggingState() {
            _isDragging = false;
            _dragItem = null;
        }

        private void PackageFolderContextMenu_Opened(object sender, RoutedEventArgs e) {
            // dynamically add predefined framework folders
            ContextMenu menu = (ContextMenu)sender;
            AddFrameworkFoldersToContextMenu(menu);
            menu.Opened -= PackageFolderContextMenu_Opened;
        }

        private static readonly Dictionary<string, string[]> _frameworkFolders =
           new Dictionary<string, string[]>() {
               { "Windows Phone", new[] { "v7.0", "sl3-wp", "v7.1 (Mango)", "sl4-WindowsPhone71" }},
               { "Siverlight", new[] {"(no version)", "sl", "v2.0", "sl2", "v3.0", "sl30", "v4.0", "sl40", "v5.0", "sl50" }},
               { ".NET", new[] { "(no version)", "net", "v1.0", "net10", "v1.1", "net11", "v2.0", "net20", "v3.0", "net30", "v3.5", "net35", "v4.0", "net40"}}
           };

        private void AddFrameworkFoldersToContextMenu(ContextMenu menu) {
            Binding visibilityBinding = new Binding("Path") {
                Converter = new StringToVisibilityConverter(),
                ConverterParameter = "lib"
            };

            Binding commandBinding = new Binding("AddContentFolderCommand");

            bool addSeparator = menu.Items.Count > 0;
            if (addSeparator) {
                var separator = new Separator();
                separator.SetBinding(FrameworkElement.VisibilityProperty, visibilityBinding);
                menu.Items.Insert(0, separator);
            }

            foreach (var pair in _frameworkFolders) {
                var item = new MenuItem {
                    Header = String.Format(CultureInfo.CurrentCulture, "Add {0} folder", pair.Key),
                    Visibility = Visibility.Collapsed
                };
                item.SetBinding(FrameworkElement.VisibilityProperty, visibilityBinding);

                string[] values = pair.Value;
                for (int i = 0; i < values.Length; i += 2) {
                    MenuItem childItem = new MenuItem {
                        Header = values[i],
                        CommandParameter = values[i+1]
                    };
                    childItem.SetBinding(MenuItem.CommandProperty, commandBinding);
                    item.Items.Add(childItem);
                }

                menu.Items.Insert(0, item);
            }
        }
    }
}