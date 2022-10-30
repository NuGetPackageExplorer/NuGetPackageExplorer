using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Input;
using NuGetPackageExplorer.Types;
using NuGetPe;
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

        private static readonly List<(string displayName, string[] tfms)> FrameworkFolders = new List<(string, string[])>
            {
                //see https://docs.microsoft.com/en-us/nuget/schema/target-frameworks
                (
                    ".NET",
                    new[]
                    {
                        "v5.0","net5.0",
                        "v5.0-android", "net5.0-android",
                        "v5.0-ios", "net5.0-ios",
                        "v5.0-macos", "net5.0-macos",
                        "v5.0-tvos", "net5.0-tvos",
                        "v5.0-watchos", "net5.0-watchos",
                        "v5.0-windows", "net5.0-windows",

                        "v6.0","net6.0",
                        "v6.0-android", "net6.0-android",
                        "v6.0-ios", "net6.0-ios",
                        "v6.0-macos", "net6.0-macos",
                        "v6.0-tvos", "net6.0-tvos",
                        "v6.0-maccatalyst", "net6.0-maccatalyst",
                        "v6.0-tizen", "net6.0-tizen",
                        "v6.0-windows", "net6.0-windows",
						
						"v7.0","net7.0",
                        "v7.0-android", "net7.0-android",
                        "v7.0-ios", "net7.0-ios",
                        "v7.0-macos", "net7.0-macos",
                        "v7.0-tvos", "net7.0-tvos",
                        "v7.0-maccatalyst", "net7.0-maccatalyst",
                        "v7.0-tizen", "net7.0-tizen",
                        "v7.0-windows", "net7.0-windows",
                    }
                ),

                //see https://docs.microsoft.com/en-us/nuget/schema/target-frameworks
                (
                    ".NET Core App",
                    new[]
                    {
                        "v1.0","netcoreapp1.0",
                        "v1.1","netcoreapp1.1",
                        "v2.0","netcoreapp2.0",
                        "v2.1","netcoreapp2.1",
                        "v2.2","netcoreapp2.2",
                        "v3.0","netcoreapp3.0",
                        "v3.1","netcoreapp3.1",
                    }
                ),

                (
                    //see https://github.com/dotnet/corefx/blob/master/Documentation/architecture/net-platform-standard.md 
                    //and https://docs.microsoft.com/en-us/dotnet/articles/standard/library
                    ".NET Platform Standard",
                    new []
                    {
                        ".NET Standard 1.0","netstandard1.0",
                        ".NET Standard 1.1","netstandard1.1",
                        ".NET Standard 1.2","netstandard1.2",
                        ".NET Standard 1.3","netstandard1.3",
                        ".NET Standard 1.4","netstandard1.4",
                        ".NET Standard 1.5","netstandard1.5",
                        ".NET Standard 1.6","netstandard1.6",
                        ".NET Standard 2.0","netstandard2.0",
                        ".NET Standard 2.1","netstandard2.1",
                    }

                )
                ,

                (
                    ".NET Framework",
                    new[]
                    {
                        "(no version)", "net",
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
                        "v4.7.2", "net472",
                        "v4.8", "net48",
						"v4.8.1", "net481"
                    }
                )
                
                ,
                (
                    //see https://docs.nuget.org/ndocs/schema/target-frameworks
                    "Xamarin",
                    new[]
                    {

                        "Android", "MonoAndroid",
                        "Mac", "xamarinmac",
                        "iOS", "xamarinios",
                        "Playstation 3", "xamarinpsthree",
                        "Playstation 4", "xamarinpsfour",
                        "PS Vita", "xamarinpsvita",
                        "Watch OS", "xamarinwatchos",
                        "TV OS", "xamarintvos",
                        "XBox One", "xamarinxboxone",
                    }
                )
                ,
                (
                    "Windows UWP",
                    new[] {
                        "Windows 10", "uap10.0"
                    }
                )
                ,

                (
                    "Native",
                    new []
                    {
                        "(no version)", "native"
                    }
                ),

                (
                    "Tizen",
                    new[]
                    {
                        "v3","tizen3",
                        "v4","tizen4",
                    }
                ),

                (
                    "Silverlight",
                    new[]
                    {
                        "v5.0", "sl50"
                    }
                ),
                (
                    "Portable Library",
                    Array.Empty<string>()
                )
            };

        private readonly ISettingsManager _settings;
        private readonly IUIServices _messageBoxServices;

        private double _analysisPaneWidth = 250; // default width for package analysis pane
        private TreeViewItem? _dragItem;
        private System.Windows.Point _dragPoint;
        private bool _isDragging, _isPressing;

#pragma warning disable CS8618 // Non-nullable field is uninitialized.
        public PackageViewer(ISettingsManager settings, IUIServices messageBoxServices, IPackageChooser packageChooser)
#pragma warning restore CS8618 // Non-nullable field is uninitialized.
        {
            InitializeComponent();

            _settings = settings;
            _messageBoxServices = messageBoxServices;

            PackageMetadataEditor.UIServices = messageBoxServices;
            PackageMetadataEditor.PackageChooser = packageChooser;

            DataContextChanged += OnDataContextChanged;
        }

        private PackageFolder RootFolder
        {
            get { return ((PackageViewModel)DataContext).RootFolder; }
        }

        private void FileContentContainer_IsVisibleChanged(object sender, DependencyPropertyChangedEventArgs e)
        {
            if ((bool)e.NewValue)
            {
                ContentGrid.RowDefinitions[0].Height = new GridLength(_settings.PackageContentHeight, GridUnitType.Star);
                ContentGrid.RowDefinitions[2].Height = new GridLength(_settings.ContentViewerHeight, GridUnitType.Star);
                ContentGrid.RowDefinitions[2].MinHeight = 150;

                if (FileContentContainer.Content == null)
                {
                    FileContentContainer.Content = CreateFileContentViewer();
                }
            }
            else
            {
                _settings.PackageContentHeight = ContentGrid.RowDefinitions[0].Height.Value;
                _settings.ContentViewerHeight = ContentGrid.RowDefinitions[2].Height.Value;

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
            PackageFolder? folder;

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

                if (CanHandleDataObject(folder, data))
                {
                    effects = DragDropEffects.Copy;

                    if (data.GetDataPresent(PackageFileDataFormat))
                    {
                        var copying = (e.KeyStates & DragDropKeyStates.ControlKey) == DragDropKeyStates.ControlKey;
                        effects = copying ? DragDropEffects.Copy : DragDropEffects.Move;
                    }
                }
            }

            e.Effects = effects;
            e.Handled = true;
        }

        private void OnTreeViewItemDrop(object sender, DragEventArgs e)
        {
            PackageFolder? folder = null;

            if (sender is TreeViewItem item)
            {
                folder = item.DataContext as PackageFolder;
            }

            var copying = (e.KeyStates & DragDropKeyStates.ControlKey) == DragDropKeyStates.ControlKey;
            if (HandleDataObject(folder, e.Data, copying))
            {
                e.Handled = true;
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
                if (item.DataContext is PackagePart)
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

            try
            {
                var item = sender as TreeViewItem;
                if (item == _dragItem && item != null)
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

                            var data = CreateDataObject(packagePart);

                            DiagnosticsClient.TrackEvent("PackageViewer_BeginDragDrop");

                            DragDrop.DoDragDrop(item, data, DragDropEffects.Copy | DragDropEffects.Move);
                            ResetDraggingState();
                        }
                    }
                }
            }
            catch // Possible COM exception if already in progress, ignore
            {
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

        private void OnTreeViewItemCopy(object sender, ExecutedRoutedEventArgs e)
        {
            if (sender is TreeView treeView && treeView.SelectedItem is PackagePart packagePart)
            {
                var data = CreateDataObject(packagePart);
                Clipboard.SetDataObject(data);
            }
        }

        private void OnTreeViewItemCanPaste(object sender, CanExecuteRoutedEventArgs e)
        {
            PackageFolder? folder = null;

            if (sender is TreeView treeView)
            {
                folder = treeView.SelectedItem as PackageFolder;
            }

            e.CanExecute = CanHandleDataObject(folder, Clipboard.GetDataObject());
        }

        private void OnTreeViewItemPaste(object sender, ExecutedRoutedEventArgs e)
        {
            PackageFolder? folder = null;

            if (sender is TreeView treeView)
            {
                folder = treeView.SelectedItem as PackageFolder;
            }

            try
            {
                if (HandleDataObject(folder, Clipboard.GetDataObject(), true))
                {
                    e.Handled = true;
                }
            }
            catch (Exception ex)
            {
                // Suppress any COM errors coming from the paste
                _messageBoxServices.Show(ex.Message, MessageLevel.Error);
            }

        }

        private void OnDataContextChanged(object sender, DependencyPropertyChangedEventArgs e)
        {
            if (Clipboard.ContainsData(PackageFileDataFormat))
            {
                Clipboard.Clear();
            }
        }

        private bool CanHandleDataObject(PackageFolder? folder, IDataObject data)
        {
            if (DataContext is PackageViewModel model)
            {
                if (model.IsSigned || model.IsInEditFileMode || model.IsInEditMetadataMode)
                {
                    return false;
                }
            }

            if (data.GetDataPresent(PackageFileDataFormat))
            {
                if (data.GetData(PackageFileDataFormat) is string packagePartPath)
                {
                    var packagePart = RootFolder.GetPackageParts().FirstOrDefault(part => part.Path == packagePartPath);

                    // make sure we don't drag a file or folder into the same parent
                    if (packagePart != null &&
                        folder != null &&
                        !folder.Contains(packagePart) &&
                        !folder.ContainsFile(packagePart.Name) &&
                        !folder.ContainsFolder(packagePart.Name) &&
                        !folder.IsDescendantOf(packagePart))
                    {
                        return true;
                    }
                }
            }
            if (data.GetDataPresent(NativeDragDrop.FileGroupDescriptorW))
            {
                return true;
            }
            if (data.GetDataPresent(DataFormats.FileDrop))
            {
                return true;
            }

            return false;
        }

        private bool HandleDataObject(PackageFolder? folder, IDataObject data, bool copy)
        {
            if (!CanHandleDataObject(folder, data))
            {
                return false;
            }

            if (data.GetDataPresent(PackageFileDataFormat))
            {
                if (data.GetData(PackageFileDataFormat) is string packagePartPath)
                {
                    var packagePart = RootFolder.GetPackageParts().FirstOrDefault(part => part.Path == packagePartPath);

                    if (packagePart != null)
                    {
                        folder ??= RootFolder;

                        if (packagePart is PackageFile file)
                        {
                            folder.AddFile(file, copy);
                        }
                        else
                        {
                            if (packagePart is PackageFolder childFolder && !folder.IsDescendantOf(childFolder))
                            {
                                folder.AddFolder(childFolder, copy);
                            }
                        }
                        return true;
                    }
                }
            }
            if (data.GetDataPresent(NativeDragDrop.FileGroupDescriptorW))
            {
                folder ??= RootFolder;

                PackageViewModel.AddDraggedAndDroppedFileDescriptors(folder, NativeDragDrop.GetFileGroupDescriptorW(data));
                return true;
            }
            if (data.GetDataPresent(DataFormats.FileDrop))
            {
                var value = data.GetData(DataFormats.FileDrop);
                if (value is string[] filenames && filenames.Length > 0)
                {
                    var viewModel = (PackageViewModel)DataContext;
                    viewModel.AddDraggedAndDroppedFiles(folder, filenames);
                    return true;
                }
            }
            return false;
        }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Reliability", "CA2000:Dispose objects before losing scope", Justification = "<Pending>")]
        private static IDataObject CreateDataObject(PackagePart packagePart)
        {
            var data = new DataObject();
            data.SetData(PackageFileDataFormat, packagePart.Path);

            if (packagePart is PackageFile packageFile)
            {
                long? fileSize = null;
                if (packageFile.OriginalPath != null && File.Exists(packageFile.OriginalPath))
                {
                    // Try to get the length, it may not really exist
                    try
                    {
                        fileSize = new FileInfo(packageFile.OriginalPath).Length;
                    }
                    catch (FileNotFoundException)
                    { }
                }

                data.SetData(NativeDragDrop.FileGroupDescriptorW, NativeDragDrop.CreateFileGroupDescriptorW(packageFile.Name, packageFile.LastWriteTime, fileSize));
                data.SetData(NativeDragDrop.FileContents, new LazyPackageFileStream(packageFile));
            }

            return data;
        }

        private static void AddFrameworkFoldersToContextMenu(ContextMenu menu)
        {
            var visibilityBinding = new Binding("Path")
            {
                Converter = new StringToVisibilityConverter(),
                ConverterParameter = "lib;content;tools;build;buildMultiTargeting;buildTransitive;ref;contentFiles"
            };

            var commandBinding = new Binding("AddContentFolderCommand");



            var menuItems = new List<object>();

          


            foreach (var pair in FrameworkFolders)
            {
                var item = new MenuItem
                {
                    Header = string.Format(CultureInfo.CurrentCulture, "Add {0} folder", pair.displayName),
                    Visibility = Visibility.Collapsed
                };
                item.SetBinding(VisibilityProperty, visibilityBinding);

                var tfm = pair.tfms;
                if (tfm.Length > 2)
                {
                    for (var i = 0; i < tfm.Length; i += 2)
                    {
                        var childItem = new MenuItem
                        {
                            Header = tfm[i],
                            CommandParameter = tfm[i + 1]
                        };
                        childItem.SetBinding(MenuItem.CommandProperty, commandBinding);
                        item.Items.Add(childItem);
                    }
                }
                else if (tfm.Length == 2)
                {
                    item.SetBinding(MenuItem.CommandProperty, commandBinding);
                    item.CommandParameter = tfm[1];
                }
                else
                {
                    // HACK: portable library menu item
                    item.SetBinding(MenuItem.CommandProperty, commandBinding);
                    item.CommandParameter = "portable";
                }

                menuItems.Insert(0, item);;
            }

            var addSeparator = menu.Items.Count > 0;
            if (addSeparator)
            {
                var separator = new Separator();
                separator.SetBinding(VisibilityProperty, visibilityBinding);
                menuItems.Insert(0, separator);
            }


            // We use the list as an intermediate to reverse the order as we build it
            foreach (var item in menuItems)
            {
                menu.Items.Insert(0, item);
            }
        }

        private class LazyPackageFileStream : Stream
        {
            private readonly PackageFile _packageFile;
            private Stream? _inner;

            public LazyPackageFileStream(PackageFile packageFile)
            {
                _packageFile = packageFile;
            }

            private void InitStream()
            {
                if (_inner == null)
                {
                    var memoryStream = new MemoryStream();
                    using (var stream = _packageFile.GetStream())
                    {
                        stream.CopyTo(memoryStream);
                    }
                    _inner = memoryStream;
                }
            }

            public override bool CanRead => true;

            public override bool CanSeek => true;

            public override bool CanWrite => false;

            public override long Length
            {
                get
                {
                    InitStream();
                    Debug.Assert(_inner != null, nameof(_inner) + " != null");
                    return _inner.Length;
                }
            }

            public override long Position
            {
                get
                {
                    InitStream();
                    Debug.Assert(_inner != null, nameof(_inner) + " != null");
                    return _inner.Position;
                }
                set
                {
                    InitStream();
                    Debug.Assert(_inner != null, nameof(_inner) + " != null");
                    _inner.Position = value;
                }
            }

            public override void Flush() => throw new NotImplementedException();

            public override int Read(byte[] buffer, int offset, int count)
            {
                InitStream();

                Debug.Assert(_inner != null, nameof(_inner) + " != null");

                return _inner.Read(buffer, offset, count);
            }

            public override long Seek(long offset, SeekOrigin origin)
            {
                InitStream();

                Debug.Assert(_inner != null, nameof(_inner) + " != null");

                return _inner.Seek(offset, origin);
            }

            public override void SetLength(long value) => throw new NotImplementedException();

            public override void Write(byte[] buffer, int offset, int count) => throw new NotImplementedException();

            protected override void Dispose(bool disposing)
            {
                base.Dispose(disposing);

                _inner?.Dispose();
            }
        }

    }
}
