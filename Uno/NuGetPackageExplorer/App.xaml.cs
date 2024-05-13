using Microsoft.Extensions.Logging;
using CommunityToolkit.WinUI.Helpers;

using NuGet.Common;
using NuGet.Configuration;
using NuGet.Credentials;
using NuGet.Packaging.Core;
using NuGet.Protocol;
using NuGet.Versioning;

using NuGetPackageExplorer.Extensions;
using NuGetPackageExplorer.Types;

using NuGetPe;

using NupkgExplorer.Framework.Extensions;
using NupkgExplorer.Framework.Navigation;
using NupkgExplorer.Presentation.Content;
using NupkgExplorer.Presentation.Dialogs;

using PackageExplorer;

using PackageExplorerViewModel;
using PackageExplorerViewModel.Types;

using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.ComponentModel.Composition.Hosting;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Threading.Tasks;

using Uno.Extensions;
using Uno.Logging;

using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.UI.Core;
using Windows.UI.Popups;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Navigation;

using LogLevel = Microsoft.Extensions.Logging.LogLevel;

namespace PackageExplorer
{
    /// <summary>
    /// Provides application-specific behavior to supplement the default Application class.
    /// </summary>
    public sealed partial class App : Application
    {
        public Window MainWindow { get; private set; }

        /// <summary>
        /// Initializes the singleton application object.  This is the first line of authored code
        /// executed, and as such is the logical equivalent of main() or WinMain().
        /// </summary>
        public App()
        {
            InitializeLogging();

            this.InitializeComponent();

#if !USE_WINUI
            this.Suspending += OnSuspending;
#endif

            DiagnosticsClient.Initialize(
#if __WASM__
                new NuGetPackageExplorer.Services.AppInsightsJsTelemetryService(
                    new List<ITelemetryServiceInitializer> {
                        new NuGetPe.Utility.AppVersionTelemetryInitializer(),
                        new NuGetPe.Utility.EnvironmentTelemetryInitializer()
                    })
#endif
            );
        }

        internal static new App Current => (App)Application.Current;

        private CompositionContainer _container;

        [SuppressMessage("Microsoft.Reliability", "CA2000:Dispose objects before losing scope")]
        internal CompositionContainer Container
        {
            get
            {
                if (_container == null)
                {
                    var catalog1 = new AssemblyCatalog(typeof(App).Assembly);
                    var catalog2 = new AssemblyCatalog(typeof(PackageViewModel).Assembly);
                    var catalog = new AggregateCatalog(catalog1, catalog2);

                    _container = new CompositionContainer(catalog);

                    // add PluginManager instance to be available as export to the rest of the app.
                    _container.ComposeParts(new PluginManager(catalog));
                }

                return _container;
            }
        }

        /// <summary>
        /// Invoked when the application is launched normally by the end user.  Other entry points
        /// will be used such as when the application is launched to open a specific file.
        /// </summary>
        /// <param name="e">Details about the launch request and process.</param>
        protected override async void OnLaunched(LaunchActivatedEventArgs e) =>
            //await OnLaunched<MainWindow>(e, Container.GetExportedValue<MainWindow>, PerformMainLandingNavigation)
            await OnLaunched<Shell>(e, BuildShell, PerformShellLandingNavigation)
                .ConfigureAwait(true);

        private async Task OnLaunched<TRootPage>(LaunchActivatedEventArgs e, Func<TRootPage?> buildRoot, Func<TRootPage, LaunchActivatedEventArgs, Task> landingNavigation) where TRootPage : UIElement
        {
            if (buildRoot == null) throw new ArgumentNullException(nameof(buildRoot));
            if (landingNavigation == null) throw new ArgumentNullException(nameof(landingNavigation));

#if DEBUG
            if (System.Diagnostics.Debugger.IsAttached)
            {
                // this.DebugSettings.EnableFrameRateCounter = true;
            }
#endif

            InitializeContainer();
            RegisterShellNavigation();

#if NET5_0_OR_GREATER && WINDOWS
            var window = new Window();
            window.Activate();
#else
            var window = Microsoft.UI.Xaml.Window.Current;
#endif
            MainWindow = window;

            // Do not repeat app initialization when the Window already has content,
            // just ensure that the window is active
            if (!(window.Content is TRootPage rootPage))
            {
                rootPage = buildRoot()!;

                // Place the frame in the current Window
                window.Content = rootPage;
            }

#if !(NET5_0_OR_GREATER && WINDOWS)
            if (e.UWPLaunchActivatedEventArgs.PrelaunchActivated == false)
#endif
            {
                // Ensure the current window is active
                window.Activate();
            }

            try
            {
                await landingNavigation(rootPage, e).ConfigureAwait(true);
            }
            catch (Exception ex)
            {
                if (this.Log().IsEnabled(LogLevel.Error))
                {
                    this.Log().Error("Failed to perform landing navigation:", ex);

                }
                throw;
            }
        }

        private Shell BuildShell()
        {
            var shell = Container.GetExportedValue<Shell>();
            var frame = shell.GetContentFrame();
            frame.Navigated += (s, e) =>
            {
                var page = (Page)e.Content;

                page.DataContext = e.Parameter;
                Container.SatisfyImportsOnce(page);

                var vm = (e.Parameter as NupkgExplorer.Framework.MVVM.ViewModelBase);

#if HAS_UNO
                Windows.UI.ViewManagement.ApplicationView.GetForCurrentView().Title = vm?.Title ?? NuGetPackageExplorer.Constants.AppName;
#if __WASM__
                NuGetPackageExplorer.Helpers.ApplicationHelper.ReplaceUrl(new Uri(vm?.Location ?? "/", UriKind.Relative));
#endif
#endif

                DiagnosticsClient.TrackPageView(e.Content.GetType().Name);
            };
            frame.NavigationFailed += (s, e) => throw new Exception($"Failed to load {e.SourcePageType.FullName}: {e.Exception}");

            var service = Container.GetExportedValue<NavigationService>();

#if WINDOWS_UWP || __WASM__
            var manager = SystemNavigationManager.GetForCurrentView();
			// wire-up back navigation
			manager.BackRequested += (s, e) => frame.GoBack();
			frame.RegisterPropertyChangedCallback(Frame.CanGoBackProperty, (s, e) =>
			{
				manager.AppViewBackButtonVisibility = frame.CanGoBack
					? AppViewBackButtonVisibility.Visible
					: AppViewBackButtonVisibility.Collapsed;
			});
#endif

#if __WASM__
            var proxyUri = new Uri(Environment.GetEnvironmentVariable("MSDL_PROXY_LOCATION")!, UriKind.RelativeOrAbsolute);
            if (!proxyUri.IsAbsoluteUri)
            {
                var baseLocation = NuGetPackageExplorer.Helpers.ApplicationHelper.GetApplicationBaseLocation();
                Environment.SetEnvironmentVariable("MSDL_PROXY_LOCATION", $"{baseLocation.AbsoluteUri}{proxyUri.OriginalString}", EnvironmentVariableTarget.Process);
            }
#endif

            // wire-up navigation
            service.Navigated += (s, e) => frame.Navigate(e.PageType, e.ViewModel);

            return shell;
        }

        private async Task PerformShellLandingNavigation(Shell shell, LaunchActivatedEventArgs e)
        {
            var navigation = Container.GetExportedValue<NavigationService>()!;
            var deeplink = default(object?);

            try
            {
                deeplink = ParseDeepLink(e);

                if (this.Log().IsEnabled(LogLevel.Debug))
                {
                    this.Log().Debug("handling deeplink: " + (deeplink ?? "<null>"));
                }

                if (deeplink is FileInfo file)
                {
                    if (!FileUtility.IsSupportedFile(file.FullName)) throw new InvalidOperationException("Unsupported file type: " + file.Extension);
                    if (!file.Exists) throw new FileNotFoundException("No such file", file.FullName);

                    var vm = await InspectPackageViewModel.CreateFromLocalPackage(file.FullName);

                    navigation.NavigateTo(vm);

                    DiagnosticsClient.TrackEvent("AppStart", new Dictionary<string, string> { { "launchType", "filePath" } });
                }
                else if (deeplink is string searchTerm)
                {
                    var vm = new FeedPackagePickerViewModel(searchTerm);

                    navigation.NavigateTo(vm);

                    DiagnosticsClient.TrackEvent("AppStart", new Dictionary<string, string> { { "launchType", "searchTerm" } });
                }
                else if (deeplink is PackageIdentity identity)
                {
                    var vm = await InspectPackageViewModel.CreateFromRemotePackageWithFallback(identity);

                    navigation.NavigateTo(vm);

                    DiagnosticsClient.TrackEvent("AppStart", new Dictionary<string, string> { { "launchType", "packageIdentity" } });
                }
                else
                {
                    navigation.NavigateTo<FeedPackagePickerViewModel>();

                    DiagnosticsClient.TrackEvent("AppStart", new Dictionary<string, string> { { "launchType", "nugetOrgFeed" } });
                }
            }
            catch (PackageNotFoundException ex)
            {
                DiagnosticsClient.TrackException(ex);

                await new MessageDialog(ex.Message, nameof(PackageNotFoundException)).ShowAsync();

                if (deeplink is not PackageIdentity identity) throw new InvalidOperationException();

                var vm = new FeedPackagePickerViewModel(identity.Id);

                navigation.NavigateTo(vm);
            }
            catch (Exception ex)
            {
                DiagnosticsClient.TrackException(ex);

                if (this.Log().IsEnabled(LogLevel.Error))
                {
                    this.Log().Error("landing navigation failed:", ex);
                    this.Log().Error("attempting home navigatation instead...");
                }

                navigation.NavigateTo<FeedPackagePickerViewModel>();
            }
        }

        private async Task PerformMainLandingNavigation(MainWindow window, LaunchActivatedEventArgs e)
        {
            var deeplink = ParseDeepLink(e);
            if (this.Log().IsEnabled(LogLevel.Debug))
            {
                typeof(App).Log().Debug("handling deeplink: " + (deeplink ?? "<null>"));
            }

            if (deeplink is FileInfo file)
            {
                if (!FileUtility.IsSupportedFile(file.FullName)) throw new InvalidOperationException("Unsupported file type: " + file.Extension);
                if (!file.Exists) throw new FileNotFoundException("No such file", file.FullName);

                await window.OpenLocalPackage(file.FullName);
            }
            else if (deeplink is string searchTerm)
            {
                await window.OpenPackageFromRepository(searchTerm);
            }
            else if (deeplink is PackageIdentity identity)
            {
                await window.DownloadAndOpenDataServicePackage(
                    NuGetPackageExplorer.Constants.NuGetOrgSource,
                    identity.Id,
                    identity.Version
                );
            }
        }

        /// <returns>
        /// Returns any of the following:
        /// - null: open landing page (failed to match anything)
        /// - string: open search with that search term (which can be empty)
        /// - FileInfo: [debug-only] open the embedded local package
        /// - PackageIdentity: open that package from online feed
        /// </returns>
        private object? ParseDeepLink(LaunchActivatedEventArgs e)
        {
            try
            {
                object? DefaultFallbackResult()
                {
#pragma warning disable CS0162 // Unreachable code detected
#if DEBUG
                    //return "uno"; // search
                    //return new PackageIdentity("Newtonsoft.Json", NuGetVersion.Parse("13.0.1"));
                    //return new PackageIdentity("Microsoft.Extensions.DependencyInjection", NuGetVersion.Parse("5.0.1"));
                    //return new PackageIdentity("XyWpf.Library", NuGetVersion.Parse("1.0.1"));
#endif

                    return null; // landing page
#pragma warning restore CS0162 // Unreachable code detected
                }

#if __WASM__
                var location = Uno.Foundation.WebAssemblyRuntime.InvokeJS("window.location");
                if (this.Log().IsEnabled(LogLevel.Debug))
                {
                    this.Log().Debug("parsing url: " + location);
                }

                var uri = new Uri(location);
                var subpaths = uri.Segments
                    .Skip(1) // skip first item that is just "/"
                    .Select(x => x.TrimEnd('/')) // remove segment separator
                    .ToArray();

                // Process `/packages` route
                if (subpaths.Any() && "packages".Equals(subpaths[0], StringComparison.OrdinalIgnoreCase))
                {
                    // nuget.org considers any subpath after version as invalid, eg: /packages/xyz/3.1.2/invalid
                    if (subpaths.Length >= 4)
                    {
                        throw new FormatException($"Invalid path: {location}");
                    }

                    // Try to extract a direct package link, like: /packages/xyz, /packages/xyz/3.1.2
                    if (subpaths.Length >= 2)
                    {
                        var id = subpaths[1];
                        var version = default(NuGetVersion);
                        if (subpaths.Length >= 3 && !NuGetVersion.TryParse(subpaths[2], out version))
                        {
                            throw new FormatException($"Invalid version: {subpaths[2]}");
                        }

                        return new PackageIdentity(id, version);
                    }

                    // Or, a search query, like: /packages, /packages?q=uno
                    else
                    {
                        var query = new QueryParameterCollection(location)
                            .Aggregate(
                                new Dictionary<string, string>(StringComparer.InvariantCultureIgnoreCase),
                                (dict, kvp) =>
                                {
                                    dict[kvp.Key] = kvp.Value;
                                    return dict;
                                }
                            );

                        // note: null is for landing page, which isn't exactly(depends on landing navigation impl) the same as the search page.
                        return query.GetValueOrDefault("q") ?? string.Empty;
                    }
                }
#endif

                if (this.Log().IsEnabled(LogLevel.Debug))
                {
                    this.Log().Debug("parsing launch arg: " + e.Arguments);
                }

                // Assume everything else should be a path
                var deeplink = e.Arguments.NullIfEmpty();
                if (!string.IsNullOrWhiteSpace(deeplink))
                {
                    return new FileInfo(deeplink);
                }

                return DefaultFallbackResult();
            }
            catch (Exception ex)
            {
                this.Log().Error("failed to process deeplink: ", ex);
                this.Log().Info("redirecting to landing page");

                return null;
            }
        }

        private void InitializeContainer()
        {
            // Overwrite settings with the real instance
            Resources["Settings"] = Container.GetExportedValue<ISettingsManager>();

            NuGet.Protocol.Core.Types.UserAgent.SetUserAgentString(new NuGet.Protocol.Core.Types.UserAgentStringBuilder("NuGet Package Explorer")
                                                       .WithOSDescription(RuntimeInformation.RuntimeIdentifier));

            InitCredentialService();
            HttpHandlerResourceV3.CredentialsSuccessfullyUsed = (uri, credentials) =>
            {
                Container.GetExportedValue<ICredentialManager>()!.Add(credentials, uri);
                InitCredentialService();
            };

            // MigrateSettings();

            //var window = Container.GetExportedValue<MainWindow>()!;
            var uiServices = Container.GetExportedValue<IUIServices>()!;
            uiServices.Initialize();
        }

        private void InitCredentialService()
        {
            Task<IEnumerable<ICredentialProvider>> getProviders()
            {
                return Task.FromResult<IEnumerable<ICredentialProvider>>(new ICredentialProvider[]
                {
                    Container.GetExportedValue<CredentialConfigProvider>()!,
                    Container.GetExportedValue<CredentialManagerProvider>()!,
                    Container.GetExportedValue<CredentialPublishProvider>()!,
                    Container.GetExportedValue<CredentialDialogProvider>()!
                });
            };

            HttpHandlerResourceV3.CredentialService =
                new Lazy<ICredentialService>(() => new CredentialService(
                                                      new AsyncLazy<IEnumerable<ICredentialProvider>>(() => getProviders()),
                                                      nonInteractive: false,
                                                      handlesDefaultCredentials: false));

        }

        private void RegisterShellNavigation()
        {
            NupkgExplorer.Framework.MVVM.ViewModelBase.DefaultContainer = Container;

            var navigation = Container.GetExportedValue<NavigationService>();
            navigation.Register<HomePage, HomePageViewModel>();
            navigation.Register<FeedPackagePicker, FeedPackagePickerViewModel>();
            navigation.Register<InspectPackage, InspectPackageViewModel>();

            var dialog = Container.GetExportedValue<DialogService>();
            dialog.Register<DownloadProgressDialog, DownloadProgressDialogViewModel>();
        }

        /// <summary>
        /// Invoked when Navigation to a certain page fails
        /// </summary>
        /// <param name="sender">The Frame which failed navigation</param>
        /// <param name="e">Details about the navigation failure</param>
        void OnNavigationFailed(object sender, NavigationFailedEventArgs e)
        {
            throw new Exception($"Failed to load {e.SourcePageType.FullName}: {e.Exception}");
        }

        /// <summary>
        /// Invoked when application execution is being suspended.  Application state is saved
        /// without knowing whether the application will be terminated or resumed with the contents
        /// of memory still intact.
        /// </summary>
        /// <param name="sender">The source of the suspend request.</param>
        /// <param name="e">Details about the suspend request.</param>
        private void OnSuspending(object sender, Windows.ApplicationModel.SuspendingEventArgs e)
        {
            var deferral = e.SuspendingOperation.GetDeferral();
            //TODO: Save application state and stop any background activity
            deferral.Complete();
        }

        /// <summary>
        /// Configures global Uno Platform logging
        /// </summary>
        internal static void InitializeLogging()
        {
            var factory = LoggerFactory.Create(builder =>
            {
#if __WASM__
                builder.AddProvider(new global::Uno.Extensions.Logging.WebAssembly.WebAssemblyConsoleLoggerProvider());
#elif __IOS__
                builder.AddProvider(new global::Uno.Extensions.Logging.OSLogLoggerProvider());
#elif NETFX_CORE
                builder.AddDebug();
#else
                builder.AddConsole();
#endif

                // Exclude logs below this level
                builder.SetMinimumLevel(LogLevel.Information);

                // Default filters for Uno Platform namespaces
                builder.AddFilter("Uno", LogLevel.Error);
                builder.AddFilter("Windows", LogLevel.Error);
                builder.AddFilter("Microsoft", LogLevel.Error);

                builder.AddFilter("Uno.UI.DataBinding.BindingPropertyHelper", LogLevel.Critical);

                builder.AddFilter("NupkgExplorer", LogLevel.Error);
                builder.AddFilter("NuGetPackageExplorer", LogLevel.Error);
                builder.AddFilter("PackageExplorer", LogLevel.Error);

#if __WASM__
                // Telemetry
                builder.AddFilter("NuGetPackageExplorer.Services.AppInsightsJsTelemetryService", LogLevel.Error);
#endif


                // Generic Xaml events
                // builder.AddFilter("Microsoft.UI.Xaml", LogLevel.Debug );
                // builder.AddFilter("Microsoft.UI.Xaml.VisualStateGroup", LogLevel.Debug );
                // builder.AddFilter("Microsoft.UI.Xaml.StateTriggerBase", LogLevel.Debug );
                // builder.AddFilter("Microsoft.UI.Xaml.UIElement", LogLevel.Debug );
                // builder.AddFilter("Microsoft.UI.Xaml.FrameworkElement", LogLevel.Trace );

                // Layouter specific messages
                // builder.AddFilter("Microsoft.UI.Xaml.Controls", LogLevel.Debug );
                // builder.AddFilter("Microsoft.UI.Xaml.Controls.Layouter", LogLevel.Debug );
                // builder.AddFilter("Microsoft.UI.Xaml.Controls.Panel", LogLevel.Debug );

                // builder.AddFilter("Windows.Storage", LogLevel.Debug );

                // Binding related messages
                // builder.AddFilter("Microsoft.UI.Xaml.Data", LogLevel.Debug );
                // builder.AddFilter("Microsoft.UI.Xaml.Data", LogLevel.Debug );

                // Binder memory references tracking
                // builder.AddFilter("Uno.UI.DataBinding.BinderReferenceHolder", LogLevel.Debug );

                // RemoteControl and HotReload related
                // builder.AddFilter("Uno.UI.RemoteControl", LogLevel.Information);

                // Debug JS interop
                // builder.AddFilter("Uno.Foundation.WebAssemblyRuntime", LogLevel.Debug );
            });

            global::Uno.Extensions.LogExtensionPoint.AmbientLoggerFactory = factory;

#if HAS_UNO
            Uno.UI.Adapter.Microsoft.Extensions.Logging.LoggingAdapter.Initialize();
#endif
        }
    }
}
