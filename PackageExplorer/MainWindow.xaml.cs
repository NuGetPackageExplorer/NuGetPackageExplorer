using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.ComponentModel.Composition;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net.NetworkInformation;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Input;
using System.Windows.Threading;
using NuGet;
using NuGetPackageExplorer.Types;
using PackageExplorer.Properties;
using PackageExplorerViewModel;
using LazyPackageCommand = System.Lazy<NuGetPackageExplorer.Types.IPackageCommand, NuGetPackageExplorer.Types.IPackageCommandMetadata>;
using StringResources = PackageExplorer.Resources.Resources;

namespace PackageExplorer {
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    [Export]
    public partial class MainWindow : Window {

        private readonly IMruManager _mruManager;

        [Import]
        public ISettingsManager SettingsManager { get; set; }

        [Import]
        public IUIServices UIServices { get; set; }

        [Import]
        public IPackageDownloader PackageDownloader { get; set; }

        [Import]
        public IPluginManager PluginManager { get; set; }

        [Import]
        public IPackageChooser PackageChooser { get; set; }

        [Import]
        public IPackageViewModelFactory PackageViewModelFactory { get; set; }

        [ImportMany(AllowRecomposition = true)]
        public ObservableCollection<LazyPackageCommand> PackageCommands {
            get {
                return PackageCommandsContainer != null ?
                    (ObservableCollection<LazyPackageCommand>)PackageCommandsContainer.Collection : 
                    null; 
            }
            set {
                if (PackageCommandsContainer != null) {
                    PackageCommandsContainer.Collection = value;
                }
            }
        }

        [Export]
        public IPackageEditorService EditorService { get; set; }

        [ImportingConstructor]
        public MainWindow(IMruManager mruManager) {
            InitializeComponent();
            
            RecentFilesMenuItem.DataContext = _mruManager = mruManager;
            RecentFilesContainer.Collection = _mruManager.Files;
        }

        protected override void OnSourceInitialized(EventArgs e) {
            base.OnSourceInitialized(e);

            try {
                LoadSettings();
            }
            catch (Exception) { }
        }

        internal void OpenLocalPackage(string packagePath) {
            if (!File.Exists(packagePath)) {
                UIServices.Show("File not found at " + packagePath, MessageLevel.Error);
                return;
            }
            PackageSourceItem.SetCurrentValue(ContentControl.ContentProperty, "Loading " + packagePath + "...");
            Dispatcher.BeginInvoke(new Action<string>(OpenLocalPackageCore), DispatcherPriority.Loaded, packagePath);
        }

        private void OpenLocalPackageCore(string packagePath) {
            IPackage package = null;

            try {
                string extension = Path.GetExtension(packagePath);
                if (extension.Equals(NuGet.Constants.PackageExtension, StringComparison.OrdinalIgnoreCase)) {
                    package = new ZipPackage(packagePath);
                }
                else if (extension.Equals(NuGet.Constants.ManifestExtension, StringComparison.OrdinalIgnoreCase)) {
                    PackageBuilder builder = new PackageBuilder(packagePath);
                    package = builder.Build();
                }
            }
            catch (Exception ex) {
                UIServices.Show(ex.Message, MessageLevel.Error);
                return;
            }

            if (package != null) {
                LoadPackage(package, packagePath, PackageType.LocalPackage);
            }
        }

        private void LoadPackage(IPackage package, string packagePath, PackageType packageType) {
            DisposeViewModel();

            if (package != null) {
                if (!HasLoadedContent<PackageViewer>()) {
                    var packageViewer = new PackageViewer(UIServices, PackageChooser);
                    var binding = new Binding("IsInEditFileMode") {
                        Converter = new BooleanToVisibilityConverter() { Inverted = true }
                    };
                    packageViewer.SetBinding(FrameworkElement.VisibilityProperty, binding);

                    MainContentContainer.Children.Add(packageViewer);

                    // HACK HACK: set the Export of IPackageMetadataEditor here
                    EditorService = packageViewer.PackageMetadataEditor;
                }

                PackageViewModel packageViewModel = PackageViewModelFactory.CreateViewModel(package, packagePath);
                packageViewModel.PropertyChanged += OnPackageViewModelPropertyChanged;

                DataContext = packageViewModel;
                if (!String.IsNullOrEmpty(packagePath)) {
                    _mruManager.NotifyFileAdded(package, packagePath, packageType);
                }
            }
        }

        private void OnPackageViewModelPropertyChanged(object sender, PropertyChangedEventArgs e) {
            var viewModel = (PackageViewModel)sender;
            if (e.PropertyName == "IsInEditFileMode") {
                if (viewModel.IsInEditFileMode) {
                    var fileEditor = new FileEditor() {
                        DataContext = viewModel.FileEditorViewModel
                    };
                    Content = fileEditor;
                }
                else {
                    Content = RootLayout;
                }
            }
        }

        private void DisposeViewModel() {
            // dispose the old view model before opening a new one.
            var currentViewModel = DataContext as PackageViewModel;
            if (currentViewModel != null) {
                currentViewModel.Dispose();
            }
        }

        private void NewMenuItem_Click(object sender, ExecutedRoutedEventArgs e) {
            bool canceled = AskToSaveCurrentFile();
            if (canceled) {
                return;
            }

            LoadPackage(new EmptyPackage(), String.Empty, PackageType.LocalPackage);
        }

        private void OpenMenuItem_Click(object sender, ExecutedRoutedEventArgs e) {
            OpenPackageFromLocal();
        }

        private void OpenFeedItem_Click(object sender, ExecutedRoutedEventArgs e) {
            string parameter = (string)e.Parameter;
            OpenPackageFromNuGetFeed(parameter);
        }

        private void OpenPackageFromLocal() {
            bool canceled = AskToSaveCurrentFile();
            if (canceled) {
                return;
            }

            string selectedFile;
            bool result = UIServices.OpenFileDialog(
                "Select File",
                StringResources.Dialog_OpenFileFilter,
                out selectedFile);

            if (result) {
                OpenLocalPackage(selectedFile);
            }
        }

        private void OpenPackageFromNuGetFeed(string searchTerm) {
            if (!NetworkInterface.GetIsNetworkAvailable()) {
                UIServices.Show(
                    PackageExplorer.Resources.Resources.NoNetworkConnection,
                    MessageLevel.Warning);
                return;
            }

            bool canceled = AskToSaveCurrentFile();
            if (canceled) {
                return;
            }

            PackageInfo selectedPackageInfo = PackageChooser.SelectPackage(searchTerm);
            if (selectedPackageInfo != null) {
                Version packageVersion = new Version(selectedPackageInfo.Version);
                IPackage cachePackage = MachineCache.Default.FindPackage(selectedPackageInfo.Id, packageVersion); ;

                Action<IPackage> processPackageAction = (package) => {
                    DataServicePackage servicePackage = selectedPackageInfo.AsDataServicePackage();
                    servicePackage.CorePackage = package;
                    LoadPackage(servicePackage, selectedPackageInfo.DownloadUrl.ToString(), PackageType.DataServicePackage);

                    // adding package to the cache, but with low priority
                    Dispatcher.BeginInvoke(
                        (Action<IPackage>)MachineCache.Default.AddPackage,
                        DispatcherPriority.ApplicationIdle,
                        package);
                };

                if (cachePackage == null || cachePackage.GetHash() != selectedPackageInfo.PackageHash) {
                    PackageDownloader.Download(
                        selectedPackageInfo.DownloadUrl,
                        selectedPackageInfo.Id,
                        packageVersion,
                        processPackageAction
                    );
                }
                else {
                    processPackageAction(cachePackage);
                }
            }
        }

        #region Drag & drop

        private void Window_DragOver(object sender, DragEventArgs e) {
            var data = e.Data;
            if (data.GetDataPresent(DataFormats.FileDrop)) {
                object value = data.GetData(DataFormats.FileDrop);
                string[] filenames = value as string[];
                if (filenames != null && filenames.Length > 0) {
                    string firstFile = filenames[0];
                    if (FileUtility.IsSupportedFile(firstFile)) {
                        e.Effects = DragDropEffects.Copy;
                        e.Handled = true;
                        return;
                    }
                }
            }

            e.Effects = DragDropEffects.None;
            e.Handled = true;
        }

        private void Window_Drop(object sender, DragEventArgs e) {
            var data = e.Data;
            if (data.GetDataPresent(DataFormats.FileDrop)) {
                object value = data.GetData(DataFormats.FileDrop);
                string[] filenames = value as string[];
                if (filenames != null && filenames.Length > 0) {
                    string firstFile = filenames.FirstOrDefault(f => FileUtility.IsSupportedFile(f));
                    if (firstFile != null) {
                        e.Handled = true;

                        bool canceled = AskToSaveCurrentFile();
                        if (!canceled) {
                            OpenLocalPackage(firstFile);
                        }
                    }
                }
            }
        }

        #endregion

        private void ExitMenuItem_Click(object sender, RoutedEventArgs e) {
            Close();
        }

        private void HelpCommandExecuted(object sender, ExecutedRoutedEventArgs e) {
            var dialog = new AboutWindow() { Owner = this };
            dialog.ShowDialog();
            e.Handled = true;
        }

        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e) {
            bool isCanceled = AskToSaveCurrentFile();
            e.Cancel = isCanceled;

            if (!isCanceled) {
                try {
                    SaveSettings();
                    DisposeViewModel();
                }
                catch (Exception) { }
            }
        }

        /// <summary>
        /// Asks user to save the current file before doing something (e.g. exit, open a new file)
        /// </summary>
        /// <returns>true if user cancels the impending action</returns>
        private bool AskToSaveCurrentFile() {
            if (HasUnsavedChanges) {

                // if there is unsaved changes, ask user for confirmation
                var result = UIServices.ConfirmWithCancel(StringResources.Dialog_SaveQuestion, "You have unsaved changes in the current file.");
                if (result == null) {
                    return true;
                }

                if (result == true) {
                    var saveCommand = SaveMenuItem.Command;
                    const string parameter = "ForceSave";
                    saveCommand.Execute(parameter);
                }
            }

            return false;
        }

        private bool HasUnsavedChanges {
            get {
                var viewModel = (PackageViewModel)DataContext;
                return (viewModel != null && viewModel.HasEdit);
            }
        }

        private bool IsInEditFileMode {
            get {
                var viewModel = (PackageViewModel)DataContext;
                return (viewModel != null && viewModel.IsInEditFileMode);
            }
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

        private void LoadSettings() {
            Settings settings = Properties.Settings.Default;
            SetFontSize(settings.FontSize);
            this.LoadWindowPlacementFromSettings(settings.WindowPlacement);
        }

        private void SaveSettings() {
            Settings settings = Properties.Settings.Default;
            settings.WindowPlacement = this.SaveWindowPlacementToSettings();
        }

        private void OpenExternalLink(object sender, ExecutedRoutedEventArgs e) {
            if (e.Command != NavigationCommands.GoToPage) {
                return;
            }

            Uri uri = e.Parameter as Uri;
            if (uri == null) {
                string url = (string)e.Parameter;
                Uri.TryCreate(url, UriKind.Absolute, out uri);
            }

            if (uri != null) {
                UriHelper.OpenExternalLink(uri);
            }
        }

        private void CloseMenuItem_Click(object sender, ExecutedRoutedEventArgs e) {
            bool isCanceled = AskToSaveCurrentFile();
            if (isCanceled) {
                return;
            }

            DataContext = null;
        }

        private void CanExecuteCloseCommand(object sender, CanExecuteRoutedEventArgs e) {
            e.CanExecute = DataContext != null;
            e.Handled = true;
        }

        private void RecentFileMenuItem_Click(object sender, RoutedEventArgs e) {
            bool canceled = AskToSaveCurrentFile();
            if (canceled) {
                return;
            }

            MenuItem menuItem = (MenuItem)sender;
            var mruItem = menuItem.DataContext as MruItem;
            if (mruItem == null) {
                _mruManager.Clear();
            }
            else {
                if (mruItem.PackageType == PackageType.LocalPackage) {
                    OpenLocalPackage(mruItem.Path);
                }
                else {
                    DownloadAndOpenDataServicePackage(mruItem);
                }
            }
        }

        private void DownloadAndOpenDataServicePackage(MruItem item) {
            if (!NetworkInterface.GetIsNetworkAvailable()) {
                UIServices.Show(
                    PackageExplorer.Resources.Resources.NoNetworkConnection,
                    MessageLevel.Warning);
                return;
            }

            Uri downloadUrl;
            if (Uri.TryCreate(item.Path, UriKind.Absolute, out downloadUrl)) {
                PackageDownloader.Download(
                    downloadUrl,
                    item.Id,
                    item.Version,
                    package => LoadPackage(package, item.Path, PackageType.DataServicePackage)
                );
            }
        }

        private void AddPluginFromAssembly_Click(object sender, RoutedEventArgs e) {
            var dialog = new PluginManagerDialog() {
                Owner = this,
                DataContext = PackageViewModelFactory.CreatePluginManagerViewModel()
            };
            dialog.ShowDialog();
        }

        private bool HasLoadedContent<T>() {
            return MainContentContainer.Children.Cast<UIElement>().Any(p => p is T);
        }

        private void CanExecuteNewCommand(object sender, CanExecuteRoutedEventArgs e) {
            e.CanExecute = !IsInEditFileMode;
            e.Handled = true;
        }
    }
}
