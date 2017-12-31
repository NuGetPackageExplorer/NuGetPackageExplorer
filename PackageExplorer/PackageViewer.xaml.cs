using System;
using System.Collections.Generic;
using System.Globalization;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Input;
using NuGetPackageExplorer.Types;
using PackageExplorer.Properties;
using PackageExplorerViewModel;

namespace PackageExplorer
{
    /// <summary>
    /// Interaction logic for PackageViewer.xaml
    /// </summary>
    public partial class PackageViewer : UserControl
    {
        private const string PackageFileDataFormat = "PackageFileContent";

        private static readonly Dictionary<string, string[]> _frameworkFolders =
            new Dictionary<string, string[]>
            {
                {
                    "Portable Library",
                    new string[0]
                },
                {
                    "Native",
                    new []
                    {
                        "(no version)", "native"
                    }
                },   
                {
                    "ASP.NET 5",
                    new []
                    {
                        "dnxcore", "dnxcore50", 
                        "dotnet5.4","dotnet5.4"
                    }
                },

                //see https://docs.microsoft.com/en-us/nuget/schema/target-frameworks
                {
                    ".NET Core App",
                    new[]
                    {
                        "v1.0","netcoreapp1.0",
                        "v1.1","netcoreapp1.1",
                        "v2.0","netcoreapp2.0",
                    }
                }
                ,

                {
                    "Tizen",
                    new[]
                    {
                        "v3","tizen3",
                        "v4","tizen4",
                    }
                }
                ,

                {
                    "Mono",
                    new[]
                    {
                        "Android", "MonoAndroid", 
                        "Mono", "Mono", 
                        "iOS", "MonoTouch", 
                        "OSX", "MonoMac"
                    }
                },
                {
                    //see https://docs.nuget.org/ndocs/schema/target-frameworks
                    "Xamarin",
                    new[]
                    {
                        "Mac", "xamarinmac", 
                        "iOS", "xamarinios",
                        "Playstation 3", "xamarinpsthree", 
                        "Playstation 4", "xamarinpsfour", 
                        "PS Vita", "xamarinpsvita",
                        "Watch OS", "xamarinwatchos",
                        "TV OS", "xamarintvos",
                        "XBox 360", "xamarinxboxthreesixty",
                        "XBox One", "xamarinxboxone", 
                    }
                },
                {
                    "Windows Phone (Windows Runtime)",
                    new []
                    {
                        "(no version)", "wpa", 
                        "v8.1", "wpa81"
                    }
                },
                  {
                    "Windows Phone (appx)", 
                    new[] {
                        "v8.1", "wpa81", 
                    }
                },
                {
                    "Windows Phone (Silverlight)", 
                    new[] {
                        "v7.0", "sl3-wp", 
                        "v7.1 (Mango)", "sl4-wp71", 
                        "v8.0", "wp8", 
                        "v8.1", "wp81"}
                },

                {
                    "Silverlight",
                    new[]
                    {
                        "(no version)", "sl", 
                        "v2.0", "sl2", 
                        "v3.0", "sl30", 
                        "v4.0", "sl40", 
                        "v5.0", "sl50"
                    }
                },
                {
                    "Windows Store", 
                    new[] {
                        "(no version)", "netcore", 
                        "Windows 8", "netcore45", 
                        "Windows 8.1", "netcore451", 
                        "Windows 10", "uap10.0", 
                    }
                },
                {
                    ".NET Client profile",
                    new []
                    {
                        "v3.5 client", "net35-client", 
                        "v4.0 client", "net40-client"
                    }
                },

                {
                    //see https://github.com/dotnet/corefx/blob/master/Documentation/architecture/net-platform-standard.md 
                    //and https://docs.microsoft.com/en-us/dotnet/articles/standard/library
                    ".NET Platform Standard",
                    new []
                    {
                        ".NET Platform Standard 1.0","netstandard1.0",
                        ".NET Platform Standard 1.1","netstandard1.1",
                        ".NET Platform Standard 1.2","netstandard1.2",
                        ".NET Platform Standard 1.3","netstandard1.3",
                        ".NET Platform Standard 1.4","netstandard1.4",
                        ".NET Platform Standard 1.5","netstandard1.5",
                        ".NET Platform Standard 1.6","netstandard1.6",
                        ".NET Platform Standard 2.0","netstandard2.0",
                    }

                }
                ,


                {
                    ".NET",
                    new[]
                    {
                        "(no version)", "net", 
                        "dotnet", "dotnet", 
                        "v1.0", "net10", 
                        "v1.1", "net11",
                        "v2.0", "net20", 
                        "v3.0", "net30", 
                        "v3.5", "net35", 
                        "v4.0", "net40", 
                        "v4.5", "net45", 
                        "v4.5.1", "net451",
                        "v4.5.2", "net452",
                        "v4.6", "net46",
                        "v4.6.1", "net461",
                        "v4.6.2", "net462",
                        "v4.7", "net47",
                        "v4.7.1", "net471",
                    }
                }
            };

        private double _analysisPaneWidth = 250; // default width for package analysis pane
        private TreeViewItem _dragItem;
        private System.Windows.Point _dragPoint;
        private bool _isDragging, _isPressing;

        public PackageViewer(IUIServices messageBoxServices, IPackageChooser packageChooser)
        {
            InitializeComponent();

            PackageMetadataEditor.UIServices = messageBoxServices;
            PackageMetadataEditor.PackageChooser = packageChooser;
        }

        private PackageFolder RootFolder
        {
            get { return (DataContext as PackageViewModel).RootFolder; }
        }

        private void FileContentContainer_IsVisibleChanged(object sender, DependencyPropertyChangedEventArgs e)
        {
            var settings = Settings.Default;

            if ((bool)e.NewValue)
            {
                ContentGrid.RowDefinitions[0].Height = new GridLength(settings.PackageContentHeight, GridUnitType.Star);
                ContentGrid.RowDefinitions[2].Height = new GridLength(settings.ContentViewerHeight, GridUnitType.Star);
                ContentGrid.RowDefinitions[2].MinHeight = 150;

                if (FileContentContainer.Content == null)
                {
                    FileContentContainer.Content = CreateFileContentViewer();
                }
            }
            else
            {
                settings.PackageContentHeight = ContentGrid.RowDefinitions[0].Height.Value;
                settings.ContentViewerHeight = ContentGrid.RowDefinitions[2].Height.Value;

                ContentGrid.RowDefinitions[2].Height = new GridLength(0, GridUnitType.Star);
                ContentGrid.RowDefinitions[2].MinHeight = 0;
            }
        }

        private void PackageAnalyzerContainer_IsVisibleChanged(object sender, DependencyPropertyChangedEventArgs e)
        {
            const double analysisPaneMinWidth = 250;

            if ((bool)e.NewValue)
            {
                var metadataWidth = ContentGrid.ColumnDefinitions[0].ActualWidth;
                var contentsWidth = ContentGrid.ColumnDefinitions[2].ActualWidth;
                var totalWidth = metadataWidth + contentsWidth;

                _analysisPaneWidth = Math.Max(_analysisPaneWidth, analysisPaneMinWidth);
                var newContentsWidth = Math.Max(
                    ContentGrid.ColumnDefinitions[2].MinWidth,
                    totalWidth - metadataWidth - _analysisPaneWidth);
                var newMetadataWidth = Math.Max(
                    ContentGrid.ColumnDefinitions[0].MinWidth,
                    totalWidth - newContentsWidth - _analysisPaneWidth);

                ContentGrid.ColumnDefinitions[0].Width = new GridLength(newMetadataWidth / totalWidth, GridUnitType.Star);
                ContentGrid.ColumnDefinitions[2].Width = new GridLength(newContentsWidth / totalWidth, GridUnitType.Star);
                ContentGrid.ColumnDefinitions[4].MinWidth = analysisPaneMinWidth;
                ContentGrid.ColumnDefinitions[4].Width = new GridLength(_analysisPaneWidth / totalWidth, GridUnitType.Star);

                if (PackageAnalyzerContainer.Content == null)
                {
                    PackageAnalyzerContainer.Content = new PackageAnalyzerPane();
                }
            }
            else
            {
                var metadataWidth = ContentGrid.ColumnDefinitions[0].ActualWidth;
                var contentsWidth = ContentGrid.ColumnDefinitions[2].ActualWidth;
                _analysisPaneWidth = ContentGrid.ColumnDefinitions[4].ActualWidth;
                var totalWidth = metadataWidth + contentsWidth + _analysisPaneWidth;

                var newContentsWidth = contentsWidth + _analysisPaneWidth;

                ContentGrid.ColumnDefinitions[0].Width = new GridLength(metadataWidth / totalWidth, GridUnitType.Star);
                ContentGrid.ColumnDefinitions[2].Width = new GridLength(newContentsWidth / totalWidth, GridUnitType.Star);
                ContentGrid.ColumnDefinitions[4].MinWidth = 0;
                ContentGrid.ColumnDefinitions[4].Width = new GridLength(0, GridUnitType.Star);
            }
        }

        // delay load the Syntax HighlightTextBox, avoid loading SyntaxHighlighting.dll upfront
        [MethodImpl(MethodImplOptions.NoInlining)]
        private static UserControl CreateFileContentViewer()
        {
            var content = new ContentViewerPane();
            content.SetBinding(DataContextProperty, new Binding("CurrentFileInfo"));
            return content;
        }

        private void OnTreeViewSelectedItemChanged(object sender, RoutedPropertyChangedEventArgs<object> e)
        {
            if (DataContext is PackageViewModel model)
            {
                model.SelectedItem = PackagesTreeView.SelectedItem;
            }
        }

        private void OnTreeViewItemDoubleClick(object sender, RoutedEventArgs args)
        {
            var item = (TreeViewItem)sender;
            if (item.DataContext is PackageFile file)
            {
                var command = ((PackageViewModel)DataContext).ViewContentCommand;
                command.Execute(file);

                args.Handled = true;
            }
        }

        private void TreeView_PreviewMouseRightButtonDown(object sender, MouseButtonEventArgs e)
        {
            var tv = (TreeView)sender;
            var element = tv.InputHitTest(e.GetPosition(tv));
            while (!((element is TreeView) || element == null))
            {
                if (element is TreeViewItem)
                {
                    break;
                }

                if (element is FrameworkElement fe)
                {
                    element = (IInputElement)(fe.Parent ?? fe.TemplatedParent);
                }
                else if (element is FrameworkContentElement fce)
                {
                    element = (IInputElement)fce.Parent;
                }
                else
                {
                    break;
                }
            }
            if (element is TreeViewItem)
            {
                element.Focus();
                e.Handled = true;
            }
        }

        private void OnTreeViewItemDragOver(object sender, DragEventArgs e)
        {
            PackageFolder folder;

            if (sender is TreeViewItem item)
            {
                folder = item.DataContext as PackageFolder;
            }
            else
            {
                folder = RootFolder;
            }

            var effects = DragDropEffects.None;
            if (folder != null)
            {
                var data = e.Data;
                if (data.GetDataPresent(DataFormats.FileDrop))
                {
                    effects = DragDropEffects.Copy;
                }
                else
                {
                    // make sure we don't drag a file or folder into the same parent
                    if (data.GetData(PackageFileDataFormat, false) is PackagePart packagePart &&
                        !folder.Contains(packagePart) &&
                        !folder.ContainsFile(packagePart.Name) &&
                        !folder.ContainsFolder(packagePart.Name) &&
                        !folder.IsDescendantOf(packagePart))
                    {
                        // we only allow copying file for now
                        var copying = (packagePart is PackageFile) && (e.KeyStates & DragDropKeyStates.ControlKey) == DragDropKeyStates.ControlKey;
                        effects = copying ? DragDropEffects.Copy : DragDropEffects.Move;
                    }
                }
            }

            e.Effects = effects;
            e.Handled = true;
        }

        private void OnTreeViewItemDrop(object sender, DragEventArgs e)
        {
            PackageFolder folder = null;

            if (sender is TreeViewItem item)
            {
                folder = item.DataContext as PackageFolder;
            }

            var data = e.Data;
            if (data.GetDataPresent(DataFormats.FileDrop))
            {
                var value = data.GetData(DataFormats.FileDrop);
                if (value is string[] filenames && filenames.Length > 0)
                {
                    var viewModel = DataContext as PackageViewModel;
                    viewModel.AddDraggedAndDroppedFiles(folder, filenames);
                    e.Handled = true;
                }
            }
            else if (data.GetDataPresent(PackageFileDataFormat))
            {
                if (data.GetData(PackageFileDataFormat) is PackagePart packagePart)
                {
                    folder = folder ?? RootFolder;

                    if (packagePart is PackageFile file)
                    {
                        var copying = (e.KeyStates & DragDropKeyStates.ControlKey) == DragDropKeyStates.ControlKey;
                        folder.AddFile(file, copying);
                    }
                    else
                    {
                        if (packagePart is PackageFolder childFolder && !folder.IsDescendantOf(childFolder))
                        {
                            folder.AddFolder(childFolder);
                        }
                    }

                    e.Handled = true;
                }
            }
        }

        private void PackagesTreeViewItem_PreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (_isDragging)
            {
                return;
            }

            if (sender is TreeViewItem item)
            {
                // allow dragging file and folder
                if (item.DataContext is PackagePart packagePart)
                {
                    _dragItem = item;
                    _dragPoint = e.GetPosition(item);
                    _isPressing = true;
                }
            }
        }

        private void PackagesTreeViewItem_MouseMove(object sender, MouseEventArgs e)
        {
            if (_isDragging || !_isPressing)
            {
                return;
            }

            var item = sender as TreeViewItem;
            if (item == _dragItem)
            {
                var newPoint = e.GetPosition(item);
                if (Math.Abs(newPoint.X - _dragPoint.X) >= SystemParameters.MinimumHorizontalDragDistance ||
                    Math.Abs(newPoint.Y - _dragPoint.Y) >= SystemParameters.MinimumVerticalDragDistance)
                {
                    // initiate a dragging
                    if (item.DataContext is PackagePart packagePart)
                    {
                        _isPressing = false;
                        _isDragging = true;

                        var data = new DataObject(PackageFileDataFormat, packagePart);
                        DragDrop.DoDragDrop(item, data, DragDropEffects.Copy | DragDropEffects.Move);
                        ResetDraggingState();
                    }
                }
            }
        }

        private void PackagesTreeViewItem_PreviewMouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            _isPressing = false;
            if (_isDragging)
            {
                ResetDraggingState();
            }
        }

        private void ResetDraggingState()
        {
            _isPressing = false;
            _isDragging = false;
            _dragItem = null;
        }

        private void PackageFolderContextMenu_Opened(object sender, RoutedEventArgs e)
        {
            // dynamically add predefined framework folders
            var menu = (ContextMenu)sender;
            AddFrameworkFoldersToContextMenu(menu);
            menu.Opened -= PackageFolderContextMenu_Opened;
        }

        private static void AddFrameworkFoldersToContextMenu(ContextMenu menu)
        {
            var visibilityBinding = new Binding("Path")
                                    {
                                        Converter = new StringToVisibilityConverter(),
                                        ConverterParameter = "lib;content;tools;build;ref;contentFiles"
                                    };

            var commandBinding = new Binding("AddContentFolderCommand");

            var addSeparator = menu.Items.Count > 0;
            if (addSeparator)
            {
                var separator = new Separator();
                separator.SetBinding(VisibilityProperty, visibilityBinding);
                menu.Items.Insert(0, separator);
            }

            foreach (var pair in _frameworkFolders)
            {
                var item = new MenuItem
                           {
                               Header = string.Format(CultureInfo.CurrentCulture, "Add {0} folder", pair.Key),
                               Visibility = Visibility.Collapsed
                           };
                item.SetBinding(VisibilityProperty, visibilityBinding);

                var values = pair.Value;
                if (values.Length > 2)
                {
                    for (var i = 0; i < values.Length; i += 2)
                    {
                        var childItem = new MenuItem
                                        {
                                            Header = values[i],
                                            CommandParameter = values[i + 1]
                                        };
                        childItem.SetBinding(MenuItem.CommandProperty, commandBinding);
                        item.Items.Add(childItem);
                    }
                }
                else if (values.Length == 2)
                {
                    item.SetBinding(MenuItem.CommandProperty, commandBinding);
                    item.CommandParameter = values[1];
                }
                else
                {
                    // HACK: portable library menu item
                    item.SetBinding(MenuItem.CommandProperty, commandBinding);
                    item.CommandParameter = "portable";
                }

                menu.Items.Insert(0, item);
            }
        }
    }
}
