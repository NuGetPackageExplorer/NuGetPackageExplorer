using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.Composition;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net.NetworkInformation;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Threading.Tasks;

using NuGet.Packaging;
using NuGet.Versioning;

using NuGetPackageExplorer.MefServices;
using NuGetPackageExplorer.Types;

using NuGetPe;

using PackageExplorerViewModel;

using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.UI.Core;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Navigation;

using Constants = NuGetPe.Constants;
using StringResources = PackageExplorer.Resources;

// The Blank Page item template is documented at http://go.microsoft.com/fwlink/?LinkId=402352&clcid=0x409

namespace PackageExplorer
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    [Export]
    public sealed partial class MainWindow : Page
	{
        private readonly IMruManager _mruManager;

        [ImportingConstructor]
        public MainWindow(IMruManager mruManager)
        {
            _mruManager = mruManager ?? throw new ArgumentNullException(nameof(mruManager));

            this.InitializeComponent();

            //RecentFilesMenuItem.DataContext = _mruManager = mruManager;
            //RecentFilesContainer.Collection = _mruManager.Files;

            //if (AppCompat.IsWindows10S)
            //{
            //    pluginMenuItem.Visibility = Visibility.Collapsed;
            //    pluginMenuItem.IsEnabled = false;
            //    mnuPluginSep.Visibility = Visibility.Collapsed;
            //}

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

        private string? _tempFile;


        internal async Task OpenLocalPackage(string packagePath)
        {
            DiagnosticsClient.TrackEvent("MainWindow_OpenLocalPackage");

            if (!File.Exists(packagePath))
            {
                UIServices.Show("File not found at " + packagePath, MessageLevel.Error);
                return;
            }

            //var oldContent = PackageSourceItem.Content;
            //PackageSourceItem.SetCurrentValue(ContentProperty, "Loading " + packagePath + "...");

            await Dispatcher.RunAsync(
                CoreDispatcherPriority.Normal,
                () => OpenLocalPackageCore(packagePath));

            //if (!succeeded)
            //{
            //    // restore old content
            //    PackageSourceItem.SetCurrentValue(ContentProperty, oldContent);
            //}
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
                if (Constants.PackageExtension.Equals(extension, StringComparison.OrdinalIgnoreCase) ||
                    Constants.SymbolPackageExtension.Equals(extension, StringComparison.OrdinalIgnoreCase))
                {
                    DiagnosticsClient.TrackPageView("View Existing Package");
#pragma warning disable CA2000 // Dispose objects before losing scope
                    package = new ZipPackage(tempFile);
#pragma warning restore CA2000 // Dispose objects before losing scope
                }
                else if (Constants.ManifestExtension.Equals(extension, StringComparison.OrdinalIgnoreCase))
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
                package?.Dispose();
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

        internal async Task OpenPackageFromRepository(string searchTerm)
        {
            //var canceled = AskToSaveCurrentFile();
            //if (canceled)
            //{
            //    return;
            //}

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

            async Task processPackageAction(ISignaturePackage package)
            {
                LoadPackage(package,
                            package.Source,
                            repository!.PackageSource.Source,
                            PackageType.RemotePackage);

                // adding package to the cache, but with low priority
                //return Dispatcher.BeginInvoke(
                //    (Action<IPackage>)MachineCache.Default.AddPackage,
                //    DispatcherPriority.ApplicationIdle,
                //    package);
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

        internal async Task DownloadAndOpenDataServicePackage(string packageUrl, string? id = null, NuGetVersion? version = null)
        {
            //if (!NetworkInterface.GetIsNetworkAvailable())
            //{
            //    UIServices.Show(
            //        StringResources.NoNetworkConnection,
            //        MessageLevel.Warning);
            //    return;
            //}

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

        private void OnPackageViewModelPropertyChanged(object? sender, PropertyChangedEventArgs e)
        {
            var viewModel = (PackageViewModel)sender!;
            if (e.PropertyName == "IsInEditFileMode")
            {
#if !HAS_UNO && !USE_WINUI
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
#endif
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

#if !HAS_UNO && !USE_WINUI
                    // HACK HACK: set the Export of IPackageMetadataEditor here
                    EditorService = packageViewer.PackageMetadataEditor;
#endif
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
                    Console.WriteLine(e);
                    UIServices.Show($"Error loading package\n{e.Message}", MessageLevel.Error);
                }
            }
        }
        private bool HasLoadedContent<T>()
        {
            return MainContentContainer.Children.Cast<UIElement>().Any(p => p is T);
        }
    }
}
