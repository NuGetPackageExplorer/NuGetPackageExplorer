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
using NuGet.Versioning;
using NuGetPe;
using NuGetPackageExplorer.Types;
using PackageExplorer.Properties;
using PackageExplorerViewModel;
using Constants = NuGetPe.Constants;
using LazyPackageCommand = System.Lazy<NuGetPackageExplorer.Types.IPackageCommand, NuGetPackageExplorer.Types.IPackageCommandMetadata>;
using StringResources = PackageExplorer.Resources.Resources;
using NuGet.Packaging;
using NuGetPe.Utility;

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
        public MainWindow(IMruManager mruManager)
        {
            InitializeComponent();

            RecentFilesMenuItem.DataContext = _mruManager = mruManager;
            RecentFilesContainer.Collection = _mruManager.Files;
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
                           ? (ObservableCollection<LazyPackageCommand>) PackageCommandsContainer.Collection
                           : null;
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

        private bool HasUnsavedChanges
        {
            get
            {
                var viewModel = (PackageViewModel) DataContext;
                return (viewModel != null && viewModel.HasEdit);
            }
        }

        private bool IsInEditFileMode
        {
            get
            {
                var viewModel = (PackageViewModel) DataContext;
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
            catch (Exception)
            {
            }
        }

        internal async Task OpenLocalPackage(string packagePath)
        {
            if (!File.Exists(packagePath))
            {
                UIServices.Show("File not found at " + packagePath, MessageLevel.Error);
                return;
            }

            object oldContent = PackageSourceItem.Content;
            PackageSourceItem.SetCurrentValue(ContentProperty, "Loading " + packagePath + "...");

            bool succeeded = await Dispatcher.InvokeAsync(
                () => OpenLocalPackageCore(packagePath), DispatcherPriority.Loaded);

            if (!succeeded)
            {
                // restore old content
                PackageSourceItem.SetCurrentValue(ContentProperty, oldContent);
            }
        }

        private bool OpenLocalPackageCore(string packagePath)
        {
            IPackage package = null;

            try
            {
                string extension = Path.GetExtension(packagePath);
                if (extension.Equals(Constants.PackageExtension, StringComparison.OrdinalIgnoreCase))
                {
                    package = new ZipPackage(packagePath);
                }
                else if (extension.Equals(Constants.ManifestExtension, StringComparison.OrdinalIgnoreCase))
                {
                    using (var str = ManifestUtility.ReadManifest(packagePath))
                    {
                        var builder = new PackageBuilder(str, Path.GetDirectoryName(packagePath));
                        package = builder.Build();
                    }
                }

                if (package != null)
                {
                    LoadPackage(package, packagePath, PackageType.LocalPackage);
                    return true;
                }
            }
            catch (Exception ex)
            {
                UIServices.Show(ex.Message, MessageLevel.Error);
                return false;
            }

            return false;
        }

        private async void LoadPackage(IPackage package, string packagePath, PackageType packageType)
        {
            DisposeViewModel();

            if (package != null)
            {
                if (!HasLoadedContent<PackageViewer>())
                {
                    var packageViewer = new PackageViewer(UIServices, PackageChooser);
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
                    PackageViewModel packageViewModel = await PackageViewModelFactory.CreateViewModel(package, packagePath);
                    packageViewModel.PropertyChanged += OnPackageViewModelPropertyChanged;

                    DataContext = packageViewModel;
                    if (!String.IsNullOrEmpty(packagePath))
                    {
                        _mruManager.NotifyFileAdded(package, packagePath, packageType);
                    }
                }
                catch (Exception e)
                {
                    UIServices.Show($"Error loading package\n{e.Message}", MessageLevel.Error);
                }
            }
        }

        private void OnPackageViewModelPropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            var viewModel = (PackageViewModel) sender;
            if (e.PropertyName == "IsInEditFileMode")
            {
                if (viewModel.IsInEditFileMode)
                {
                    var fileEditor = new FileEditor
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
            var currentViewModel = DataContext as PackageViewModel;
            if (currentViewModel != null)
            {
                currentViewModel.PropertyChanged -= OnPackageViewModelPropertyChanged;
                currentViewModel.Dispose();
            }
        }

        private void NewMenuItem_Click(object sender, ExecutedRoutedEventArgs e)
        {
            bool canceled = AskToSaveCurrentFile();
            if (canceled)
            {
                return;
            }

            LoadPackage(new EmptyPackage(), String.Empty, PackageType.LocalPackage);
        }

        private void OpenMenuItem_Click(object sender, ExecutedRoutedEventArgs e)
        {
            OpenPackageFromLocal();
        }

        private async void OpenFeedItem_Click(object sender, ExecutedRoutedEventArgs e)
        {
            var parameter = (string) e.Parameter;
            if (!String.IsNullOrEmpty(parameter))
            {
                parameter = "id:" + parameter;
            }
            await OpenPackageFromRepository(parameter);
        }

        private Task OpenPackageFromLocal()
        {
            bool canceled = AskToSaveCurrentFile();
            if (canceled)
            {
                return Task.FromResult(0);
            }

            string selectedFile;
            bool result = UIServices.OpenFileDialog(
                "Select File",
                StringResources.Dialog_OpenFileFilter,
                out selectedFile);

            if (result)
            {
                return OpenLocalPackage(selectedFile);
            }

            return Task.FromResult(0);
        }

        private async Task OpenPackageFromRepository(string searchTerm)
        {
            bool canceled = AskToSaveCurrentFile();
            if (canceled)
            {
                return;
            }

            PackageInfo selectedPackageInfo = PackageChooser.SelectPackage(searchTerm);
            if (selectedPackageInfo == null)
            {
                return;
            }

            if (selectedPackageInfo.IsLocalPackage)
            {
                await OpenLocalPackage(selectedPackageInfo.DownloadUrl.LocalPath);
            }
            else 
            {
                var packageVersion = new NuGetVersion(selectedPackageInfo.Version);
                IPackage cachePackage = MachineCache.Default.FindPackage(selectedPackageInfo.Id, packageVersion);

                Func<IPackage, DispatcherOperation> processPackageAction = (package) =>
                                                        {
                                                            DataServicePackage servicePackage = selectedPackageInfo.AsDataServicePackage();
                                                            servicePackage.CorePackage = package;
                                                            LoadPackage(servicePackage,
                                                                        selectedPackageInfo.DownloadUrl.ToString(),
                                                                        PackageType.DataServicePackage);

                                                            // adding package to the cache, but with low priority
                                                            return Dispatcher.BeginInvoke(
                                                                (Action<IPackage>) MachineCache.Default.AddPackage,
                                                                DispatcherPriority.ApplicationIdle,
                                                                package);
                                                        };

                if (cachePackage == null || cachePackage.GetHash() != selectedPackageInfo.PackageHash)
                {
                    IPackage downloadedPackage = await PackageDownloader.Download(
                        selectedPackageInfo.DownloadUrl,
                        selectedPackageInfo.Id,
                        packageVersion.ToString());

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
        }

        private void ExitMenuItem_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }

        private void HelpCommandExecuted(object sender, ExecutedRoutedEventArgs e)
        {
            var dialog = new AboutWindow {Owner = this};
            dialog.ShowDialog();
            e.Handled = true;
        }

        private void Window_Closing(object sender, CancelEventArgs e)
        {
            bool isCanceled = AskToSaveCurrentFile();
            e.Cancel = isCanceled;

            if (!isCanceled)
            {
                try
                {
                    SaveSettings();
                    DisposeViewModel();
                }
                catch (Exception)
                {
                }
            }
        }

        /// <summary>
        /// Asks user to save the current file before doing something (e.g. exit, open a new file)
        /// </summary>
        /// <returns>true if user cancels the impending action</returns>
        private bool AskToSaveCurrentFile()
        {
            var viewModel = (PackageViewModel) DataContext;
            if (HasUnsavedChanges || (IsInEditFileMode && viewModel.FileEditorViewModel.HasEdit))
            {
                // if there is unsaved changes, ask user for confirmation
                bool? result = UIServices.ConfirmWithCancel("You have unsaved changes in the current package.", StringResources.Dialog_SaveQuestion);

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
                        viewModel.FileEditorViewModel.SaveOnExit((IFileEditorService) Content);
                    }

                    ICommand saveCommand = viewModel.SaveCommand;
                    const string parameter = "ForceSave";
                    saveCommand.Execute(parameter);
                }
            }

            return false;
        }

        private void OnFontSizeItem_Click(object sender, RoutedEventArgs e)
        {
            var item = (MenuItem) sender;
            int size = Convert.ToInt32(item.Tag, CultureInfo.InvariantCulture);
            Settings.Default.FontSize = size;
        }

        private void LoadSettings()
        {
            Settings settings = Settings.Default;
            this.LoadWindowPlacementFromSettings(settings.WindowPlacement);
        }

        private void SaveSettings()
        {
            Settings settings = Settings.Default;
            settings.WindowPlacement = this.SaveWindowPlacementToSettings();
        }

        private void OpenExternalLink(object sender, ExecutedRoutedEventArgs e)
        {
            if (e.Command != NavigationCommands.GoToPage)
            {
                return;
            }

            // We might get a certificate to display instead
            if(e.Parameter is X509Certificate2 cert)
            {
                var hwnd = new WindowInteropHelper(this).Handle;
                X509Certificate2UI.DisplayCertificate(cert, hwnd);
                return;
            }

            var uri = e.Parameter as Uri;
            if (uri == null)
            {
                var url = (string) e.Parameter;
                Uri.TryCreate(url, UriKind.Absolute, out uri);
            }

            if (uri != null)
            {
                UriHelper.OpenExternalLink(uri);
            }
        }

        private void CloseMenuItem_Click(object sender, ExecutedRoutedEventArgs e)
        {
            bool isCanceled = AskToSaveCurrentFile();
            if (isCanceled)
            {
                return;
            }

            (DataContext as PackageViewModel)?.Dispose();
            DataContext = null;
        }

        private void CanExecuteCloseCommand(object sender, CanExecuteRoutedEventArgs e)
        {
            e.CanExecute = DataContext != null;
            e.Handled = true;
        }

        private async void RecentFileMenuItem_Click(object sender, RoutedEventArgs e)
        {
            bool canceled = AskToSaveCurrentFile();
            if (canceled)
            {
                return;
            }

            var menuItem = (MenuItem) sender;
            var mruItem = menuItem.DataContext as MruItem;
            if (mruItem == null)
            {
                _mruManager.Clear();
            }
            else
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
        }

        internal Task DownloadAndOpenDataServicePackage(MruItem item)
        {
            return DownloadAndOpenDataServicePackage(item.Path, item.Id, item.Version);
        }

        internal async Task DownloadAndOpenDataServicePackage(string packageUrl, string id = null, NuGetVersion version = null)
        {
            if (!NetworkInterface.GetIsNetworkAvailable())
            {
                UIServices.Show(
                    StringResources.NoNetworkConnection,
                    MessageLevel.Warning);
                return;
            }

            Uri downloadUrl;
            if (Uri.TryCreate(packageUrl, UriKind.Absolute, out downloadUrl) && downloadUrl.IsRemoteUri())
            {
                IPackage downloadedPackage = await PackageDownloader.Download(downloadUrl, id, version.ToString());
                if (downloadedPackage != null)
                {
                    LoadPackage(downloadedPackage, packageUrl, PackageType.DataServicePackage);
                }
            }
            else
            {
                UIServices.Show(
                    String.Format(
                        CultureInfo.CurrentCulture,
                        StringResources.Dialog_InvalidPackageUrl,
                        packageUrl),
                    MessageLevel.Error
                    );
            }
        }

        private void AddPluginFromAssembly_Click(object sender, RoutedEventArgs e)
        {
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
                int fontSizeDelta = e.Delta > 0 ? 2 : -2;
                int newFontSize = Settings.Default.FontSize + fontSizeDelta;
                newFontSize = Math.Max(newFontSize, 12);
                newFontSize = Math.Min(newFontSize, 18);
                Settings.Default.FontSize = newFontSize;

                e.Handled = true;
            }
        }

        private void ViewDownloadCache_Click(object sender, EventArgs args)
        {
            string cacheSource = MachineCache.Default.Source;
            if (Directory.Exists(cacheSource))
            {
                Process.Start(cacheSource);
            }
            else
            {
                UIServices.Show("The NuGet download cache does not exist.", MessageLevel.Information);
            }
        }

        private void ClearDownloadCache_Click(object sender, EventArgs args)
        {
            bool result = MachineCache.Default.Clear();
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
            IDataObject data = e.Data;
            if (data.GetDataPresent(DataFormats.FileDrop))
            {
                object value = data.GetData(DataFormats.FileDrop);
                var filenames = value as string[];
                if (filenames != null && filenames.Length > 0)
                {
                    string firstFile = filenames[0];
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
            IDataObject data = e.Data;
            if (data.GetDataPresent(DataFormats.FileDrop))
            {
                object value = data.GetData(DataFormats.FileDrop);
                var filenames = value as string[];
                if (filenames != null && filenames.Length > 0)
                {
                    string firstFile = filenames.FirstOrDefault(FileUtility.IsSupportedFile);
                    if (firstFile != null)
                    {
                        e.Handled = true;

                        bool canceled = AskToSaveCurrentFile();
                        if (!canceled)
                        {
                            await OpenLocalPackage(firstFile);
                        }
                    }
                }
            }
        }

        #endregion
    }
}