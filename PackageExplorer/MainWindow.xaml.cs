using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.ComponentModel.Composition;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net.NetworkInformation;
using System.Security.Cryptography.X509Certificates;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Threading;
using NuGet.Packaging;
using NuGet.Versioning;
using NuGetPackageExplorer.Types;
using NuGetPe;
using PackageExplorerViewModel;
using Constants = NuGetPe.Constants;
using LazyPackageCommand = System.Lazy<NuGetPackageExplorer.Types.IPackageCommand, NuGetPackageExplorer.Types.IPackageCommandMetadata>;
using StringResources = PackageExplorer.Resources;

namespace PackageExplorer
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    [Export]
    public partial class MainWindow : Window
    {
        private readonly IMruManager _mruManager;

        [ImportingConstructor]
#pragma warning disable CS8618 // Non-nullable field is uninitialized.
        public MainWindow(IMruManager mruManager)
#pragma warning restore CS8618 // Non-nullable field is uninitialized.
        {
            InitializeComponent();

            RecentFilesMenuItem.DataContext = _mruManager = mruManager;
            RecentFilesContainer.Collection = _mruManager.Files;

            if (AppCompat.IsWindows10S)
            {
                pluginMenuItem.Visibility = Visibility.Collapsed;
                pluginMenuItem.IsEnabled = false;
                mnuPluginSep.Visibility = Visibility.Collapsed;
            }

            DiagnosticsClient.TrackPageView(nameof(MainWindow));
        }

        [Import]
        public ISettingsManager SettingsManager { get; set; }

        [Import]
        public IUIServices UIServices { get; set; }

        [Import]
        public INuGetPackageDownloader PackageDownloader { get; set; }

        [Import]
        public IPluginManager PluginManager { get; set; }

        [Import]
        public IPackageChooser PackageChooser { get; set; }

        [Import]
        public IPackageViewModelFactory PackageViewModelFactory { get; set; }

        [ImportMany(AllowRecomposition = true)]
        public ObservableCollection<LazyPackageCommand> PackageCommands
        {
            get
            {
                return PackageCommandsContainer != null
                           ? (ObservableCollection<LazyPackageCommand>)PackageCommandsContainer.Collection
                           : null!;
            }
            set
            {
                if (PackageCommandsContainer != null)
                {
                    PackageCommandsContainer.Collection = value;
                }
            }
        }

        [Export]
        public IPackageEditorService EditorService { get; set; }

        private string? _tempFile;

        private bool HasUnsavedChanges
        {
            get
            {
                var viewModel = (PackageViewModel)DataContext;
                return (viewModel != null && viewModel.HasEdit);
            }
        }

        private bool IsInEditFileMode
        {
            get
            {
                var viewModel = (PackageViewModel)DataContext;
                return (viewModel != null && viewModel.IsInEditFileMode);
            }
        }

        protected override void OnSourceInitialized(EventArgs e)
        {
            base.OnSourceInitialized(e);

            try
            {
                LoadSettings();
            }
            catch (Exception ex)
            {
                DiagnosticsClient.TrackException(ex);
            }
        }

        internal async Task OpenLocalPackage(string packagePath)
        {
            DiagnosticsClient.TrackEvent("MainWindow_OpenLocalPackage");

            if (!File.Exists(packagePath))
            {
                UIServices.Show("File not found at " + packagePath, MessageLevel.Error);
                return;
            }

            var oldContent = PackageSourceItem.Content;
            PackageSourceItem.SetCurrentValue(ContentProperty, "Loading " + packagePath + "...");

            var succeeded = await Dispatcher.InvokeAsync(
                () => OpenLocalPackageCore(packagePath), DispatcherPriority.Loaded);

            if (!succeeded)
            {
                // restore old content
                PackageSourceItem.SetCurrentValue(ContentProperty, oldContent);
            }
        }

        private bool OpenLocalPackageCore(string packagePath)
        {
            IPackage? package = null;

            string? tempFile = null;
            try
            {
                tempFile = Path.GetTempFileName();
                File.Copy(packagePath, tempFile, overwrite: true);

                var extension = Path.GetExtension(packagePath);
                if (extension.Equals(Constants.PackageExtension, StringComparison.OrdinalIgnoreCase) ||
                    extension.Equals(Constants.SymbolPackageExtension, StringComparison.OrdinalIgnoreCase))
                {
                    DiagnosticsClient.TrackPageView("View Existing Package");
                    package = new ZipPackage(tempFile);
                }
                else if (extension.Equals(Constants.ManifestExtension, StringComparison.OrdinalIgnoreCase))
                {
                    DiagnosticsClient.TrackPageView("View Nuspec");
                    using var str = ManifestUtility.ReadManifest(tempFile);
                    var builder = new PackageBuilder(str, Path.GetDirectoryName(packagePath));
                    package = builder.Build();
                }

                if (package != null)
                {
                    LoadPackage(package, packagePath, packagePath, PackageType.LocalPackage);
                    _tempFile = tempFile;
                    return true;
                }
            }
            catch (Exception ex)
            {
                package = null;
                UIServices.Show(ex.Message, MessageLevel.Error);
                return false;
            }
            finally
            {
                if (package == null && tempFile != null && File.Exists(tempFile))
                {
                    try
                    {
                        File.Delete(tempFile);
                    }
                    catch { /* ignore */ }
                }
            }

            return false;
        }

        private async void LoadPackage(IPackage package, string packagePath, string packageSource, PackageType packageType)
        {
            DisposeViewModel();

            if (package != null)
            {
                if (!HasLoadedContent<PackageViewer>())
                {
                    var packageViewer = new PackageViewer(SettingsManager, UIServices, PackageChooser);
                    var binding = new Binding
                    {
                        Converter = new NullToVisibilityConverter(),
                        FallbackValue = Visibility.Collapsed
                    };
                    packageViewer.SetBinding(VisibilityProperty, binding);

                    MainContentContainer.Children.Add(packageViewer);

                    // HACK HACK: set the Export of IPackageMetadataEditor here
                    EditorService = packageViewer.PackageMetadataEditor;
                }

                try
                {
                    var packageViewModel = await PackageViewModelFactory.CreateViewModel(package, packagePath, packageSource);
                    packageViewModel.PropertyChanged += OnPackageViewModelPropertyChanged;

                    DataContext = packageViewModel;
                    if (!string.IsNullOrEmpty(packageSource))
                    {
                        _mruManager.NotifyFileAdded(package, packageSource, packageType);
                    }
                }
                catch (Exception e)
                {
                    if (!(e is ArgumentException))
                    {
                        DiagnosticsClient.TrackException(e);
                    }
                    UIServices.Show($"Error loading package\n{e.Message}", MessageLevel.Error);
                }
            }
        }

        private void OnPackageViewModelPropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            var viewModel = (PackageViewModel)sender;
            if (e.PropertyName == "IsInEditFileMode")
            {
                if (viewModel.IsInEditFileMode)
                {
                    var fileEditor = new FileEditor(SettingsManager, UIServices)
                    {
                        DataContext = viewModel.FileEditorViewModel
                    };
                    Content = fileEditor;
                }
                else
                {
                    Content = RootLayout;
                }
            }
        }

        private void DisposeViewModel()
        {
            // dispose the old view model before opening a new one.
            if (DataContext is PackageViewModel currentViewModel)
            {
                currentViewModel.PropertyChanged -= OnPackageViewModelPropertyChanged;
                currentViewModel.Dispose();
            }
            if (_tempFile != null)
            {
                if (File.Exists(_tempFile))
                {
                    try
                    {
                        File.Delete(_tempFile);
                    }
                    catch { /* ignore */ }
                }
                _tempFile = null;
            }
        }

        private void NewMenuItem_Click(object sender, ExecutedRoutedEventArgs e)
        {
            DiagnosticsClient.TrackEvent("MainWindow_NewMenuItemClick");

            var canceled = AskToSaveCurrentFile();
            if (canceled)
            {
                return;
            }

            DiagnosticsClient.TrackPageView("Edit Blank Package");
            LoadPackage(new EmptyPackage(), string.Empty, string.Empty, PackageType.LocalPackage);
        }

        private void OpenMenuItem_Click(object sender, ExecutedRoutedEventArgs e)
        {
            DiagnosticsClient.TrackEvent("MainWindow_OpenMenuItemClick");

            OpenPackageFromLocal();
        }

        private async void OpenFeedItem_Click(object sender, ExecutedRoutedEventArgs e)
        {
            DiagnosticsClient.TrackEvent("MainWindow_OpenFeedItemClick");

            var parameter = (string)e.Parameter;
            if (!string.IsNullOrEmpty(parameter))
            {
                parameter = "id:" + parameter;
            }
            await OpenPackageFromRepository(parameter);
        }

        private Task OpenPackageFromLocal()
        {
            var canceled = AskToSaveCurrentFile();
            if (canceled)
            {
                return Task.FromResult(0);
            }

            var result = UIServices.OpenFileDialog(
                "Select File",
                StringResources.Dialog_OpenFileFilter,
                out var selectedFile);

            if (result)
            {
                return OpenLocalPackage(selectedFile);
            }

            return Task.FromResult(0);
        }

        private async Task OpenPackageFromRepository(string searchTerm)
        {
            var canceled = AskToSaveCurrentFile();
            if (canceled)
            {
                return;
            }

            var selectedPackageInfo = PackageChooser.SelectPackage(searchTerm);
            if (selectedPackageInfo == null)
            {
                return;
            }

            var repository = PackageChooser.Repository;
            if (repository == null)
            {
                return;
            }

            var cachePackage = MachineCache.Default.FindPackage(selectedPackageInfo.Id, selectedPackageInfo.SemanticVersion);

            DispatcherOperation processPackageAction(ISignaturePackage package)
            {
                LoadPackage(package,
                            package.Source,
                            repository.PackageSource.Source,
                            PackageType.RemotePackage);

                // adding package to the cache, but with low priority
                return Dispatcher.BeginInvoke(
                    (Action<IPackage>)MachineCache.Default.AddPackage,
                    DispatcherPriority.ApplicationIdle,
                    package);
            }

            if (cachePackage == null)
            {
                var downloadedPackage = await PackageDownloader.Download(
                    repository,
                    selectedPackageInfo.Identity);

                if (downloadedPackage != null)
                {
                    await processPackageAction(downloadedPackage);
                }
            }
            else
            {
                await processPackageAction(cachePackage);
            }
        }

        private void ExitMenuItem_Click(object sender, RoutedEventArgs e)
        {
            DiagnosticsClient.TrackEvent("MainWindow_ExitMenuItemClick");
            Close();
        }

        private void HelpCommandExecuted(object sender, ExecutedRoutedEventArgs e)
        {
            DiagnosticsClient.TrackEvent("MainWindow_HelpCommandExecuted");

            var dialog = new AboutWindow { Owner = this };
            dialog.ShowDialog();
            e.Handled = true;
        }

        private void Window_Closing(object sender, CancelEventArgs e)
        {
            var isCanceled = AskToSaveCurrentFile();
            e.Cancel = isCanceled;

            if (!isCanceled)
            {
                try
                {
                    SaveSettings();
                    DisposeViewModel();
                }
                catch (Exception ex)
                {
                    DiagnosticsClient.TrackException(ex);
                }
            }
        }

        /// <summary>
        /// Asks user to save the current file before doing something (e.g. exit, open a new file)
        /// </summary>
        /// <returns>true if user cancels the impending action</returns>
        private bool AskToSaveCurrentFile()
        {
            var viewModel = (PackageViewModel)DataContext;
            if (HasUnsavedChanges || (IsInEditFileMode && viewModel.FileEditorViewModel?.HasEdit == true))
            {
                // if there is unsaved changes, ask user for confirmation
                var result = UIServices.ConfirmWithCancel("You have unsaved changes in the current package.", StringResources.Dialog_SaveQuestion);

                if (result == null)
                {
                    return true;
                }
                else if (result == true)
                {
                    if (IsInEditFileMode)
                    {
                        // force a Save from outside the file editor.
                        // In this case, Content is the FileEditor user control
                        viewModel.FileEditorViewModel?.SaveOnExit((IFileEditorService)Content);
                    }

                    var saveCommand = viewModel.SaveCommand;
                    const string parameter = "ForceSave";
                    saveCommand.Execute(parameter);
                }
            }

            return false;
        }

        private void OnFontSizeItem_Click(object sender, RoutedEventArgs e)
        {
            DiagnosticsClient.TrackEvent("MainWindow_OnFontSizeItemClick");

            var item = (MenuItem)sender;
            var size = Convert.ToInt32(item.Tag, CultureInfo.InvariantCulture);
            SettingsManager.FontSize = size;
        }

        private void LoadSettings()
        {
            this.LoadWindowPlacementFromSettings(SettingsManager.WindowPlacement);
        }

        private void SaveSettings()
        {
            SettingsManager.WindowPlacement = this.SaveWindowPlacementToSettings();
        }

        private void OpenExternalLink(object sender, ExecutedRoutedEventArgs e)
        {
            if (e.Command != NavigationCommands.GoToPage)
            {
                return;
            }

            // We might get a certificate to display instead
            if (e.Parameter is X509Certificate2 cert)
            {
                DiagnosticsClient.TrackEvent("DisplayCertificate");
                var hwnd = new WindowInteropHelper(this).Handle;
                X509Certificate2UI.DisplayCertificate(cert, hwnd);
                return;
            }

            var uri = e.Parameter as Uri;
            if (uri == null)
            {
                var url = (string)e.Parameter;
                Uri.TryCreate(url, UriKind.Absolute, out uri);
            }

            if (uri != null)
            {
                UriHelper.OpenExternalLink(uri);
            }
        }

        private void CloseMenuItem_Click(object sender, ExecutedRoutedEventArgs e)
        {
            var isCanceled = AskToSaveCurrentFile();
            if (isCanceled)
            {
                return;
            }

            DiagnosticsClient.TrackEvent("MainWindow_CloseMenuItemClick");
            DiagnosticsClient.TrackPageView(nameof(MainWindow));

            DisposeViewModel();
            DataContext = null;
        }

        private void CanExecuteCloseCommand(object sender, CanExecuteRoutedEventArgs e)
        {
            e.CanExecute = DataContext != null;
            e.Handled = true;
        }

        private async void RecentFileMenuItem_Click(object sender, RoutedEventArgs e)
        {
            var canceled = AskToSaveCurrentFile();
            if (canceled)
            {
                return;
            }

            DiagnosticsClient.TrackEvent("MainWindow_RecentFileMenuItemClick");

            var menuItem = (MenuItem)sender;
            if (menuItem.DataContext is MruItem mruItem)
            {
                if (mruItem.PackageType == PackageType.LocalPackage)
                {
                    await OpenLocalPackage(mruItem.Path);
                }
                else
                {
                    await DownloadAndOpenDataServicePackage(mruItem);
                }
            }
            else
            {
                _mruManager.Clear();
            }
        }

        internal Task DownloadAndOpenDataServicePackage(MruItem item)
        {
            return DownloadAndOpenDataServicePackage(item.Path, item.Id, item.Version);
        }

        internal async Task DownloadAndOpenDataServicePackage(string packageUrl, string? id = null, NuGetVersion? version = null)
        {
            if (!NetworkInterface.GetIsNetworkAvailable())
            {
                UIServices.Show(
                    StringResources.NoNetworkConnection,
                    MessageLevel.Warning);
                return;
            }

            if (id != null && version != null && Uri.TryCreate(packageUrl, UriKind.Absolute, out _))
            {
                var repository = PackageRepositoryFactory.CreateRepository(packageUrl);
                var packageIdentity = new NuGet.Packaging.Core.PackageIdentity(id, version);

                var downloadedPackage = await PackageDownloader.Download(repository, packageIdentity);
                if (downloadedPackage != null)
                {
                    DiagnosticsClient.TrackPageView("View Feed Package");
                    LoadPackage(downloadedPackage, downloadedPackage.Source, packageUrl, PackageType.RemotePackage);
                }
            }
            else
            {
                UIServices.Show(
                    string.Format(
                        CultureInfo.CurrentCulture,
                        StringResources.Dialog_InvalidPackageUrl,
                        packageUrl),
                    MessageLevel.Error
                    );
            }
        }

        private void AddPluginFromAssembly_Click(object sender, RoutedEventArgs e)
        {
            DiagnosticsClient.TrackEvent("MainWindow_AddPluginFromAssemblyClick");

            var dialog = new PluginManagerDialog
            {
                Owner = this,
                DataContext = PackageViewModelFactory.CreatePluginManagerViewModel()
            };
            dialog.ShowDialog();
        }

        private bool HasLoadedContent<T>()
        {
            return MainContentContainer.Children.Cast<UIElement>().Any(p => p is T);
        }

        private void CanExecuteNewCommand(object sender, CanExecuteRoutedEventArgs e)
        {
            e.CanExecute = !IsInEditFileMode;
            e.Handled = true;
        }

        private void Window_PreviewMouseWheel(object sender, MouseWheelEventArgs e)
        {
            // if the Control key (and only Control key) is pressed 
            if ((Keyboard.Modifiers & ModifierKeys.Control) == ModifierKeys.Control)
            {
                DiagnosticsClient.TrackEvent("MainWindow_MouseWheelFontSize");

                var fontSizeDelta = e.Delta > 0 ? 2 : -2;
                var newFontSize = SettingsManager.FontSize + fontSizeDelta;
                newFontSize = Math.Max(newFontSize, 12);
                newFontSize = Math.Min(newFontSize, 18);
                SettingsManager.FontSize = newFontSize;

                e.Handled = true;
            }
        }

        private void ViewDownloadCache_Click(object sender, EventArgs args)
        {
            DiagnosticsClient.TrackEvent("MainWindow_ViewDownloadCache");

            var cacheSource = MachineCache.Default.Source;
            try
            {
                Process.Start("explorer.exe", cacheSource.FullName);
            }
            catch // Possible Win32 exception, nothing we can do
            {
            }
        }

        private void ClearDownloadCache_Click(object sender, EventArgs args)
        {
            DiagnosticsClient.TrackEvent("MainWindow_ClearDownloadCache");

            var result = MachineCache.Default.Clear();
            if (result)
            {
                UIServices.Show("The NuGet download cache has been cleared successfully.", MessageLevel.Information);
            }
            else
            {
                UIServices.Show("The NuGet download cache does not exist.", MessageLevel.Information);
            }
        }

        #region Drag & drop

        private void Window_DragOver(object sender, DragEventArgs e)
        {
            var data = e.Data;
            if (data.GetDataPresent(DataFormats.FileDrop))
            {
                var value = data.GetData(DataFormats.FileDrop);
                if (value is string[] filenames && filenames.Length > 0)
                {
                    var firstFile = filenames[0];
                    if (FileUtility.IsSupportedFile(firstFile))
                    {                        
                        e.Effects = DragDropEffects.Copy;
                        e.Handled = true;
                        return;
                    }
                }
            }

            e.Effects = DragDropEffects.None;
            e.Handled = true;
        }

        private async void Window_Drop(object sender, DragEventArgs e)
        {
            var data = e.Data;
            if (data.GetDataPresent(DataFormats.FileDrop))
            {
                var value = data.GetData(DataFormats.FileDrop);
                if (value is string[] filenames && filenames.Length > 0)
                {
                    var firstFile = filenames.FirstOrDefault(FileUtility.IsSupportedFile);
                    if (firstFile != null)
                    {
                        e.Handled = true;

                        var canceled = AskToSaveCurrentFile();
                        if (!canceled)
                        {
                            DiagnosticsClient.TrackEvent("MainWindow_Drop");

                            await OpenLocalPackage(firstFile);
                        }
                    }
                }
            }
        }

        #endregion
    }
}
