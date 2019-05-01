using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.ComponentModel.Composition.Hosting;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using NuGet.Common;
using NuGet.Configuration;
using NuGet.Credentials;
using NuGet.Protocol;
using NuGetPackageExplorer.Types;
using NuGetPe;
using PackageExplorerViewModel;
using PackageExplorerViewModel.Types;
using Settings = PackageExplorer.Properties.Settings;

namespace PackageExplorer
{
    public partial class App : Application
    {
#pragma warning disable CS8618 // Non-nullable field is uninitialized.
        public App()
#pragma warning restore CS8618 // Non-nullable field is uninitialized.
        {
            DiagnosticsClient.Initialize();
        }

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

        private async void Application_Startup(object sender, StartupEventArgs e)
        {
            DiagnosticsClient.TrackEvent("AppStart", new Dictionary<string, string> { { "launchType", e.Args.Length > 0 ? "fileAssociation" : "shortcut" } });

            Resources.Add("Settings", Container.GetExportedValue<ISettingsManager>());

            InitCredentialService();
            HttpHandlerResourceV3.CredentialsSuccessfullyUsed = (uri, credentials) =>
            {
                Container.GetExportedValue<ICredentialManager>().Add(credentials, uri);
                InitCredentialService();
            };

            MigrateSettings();

            var window = Container.GetExportedValue<MainWindow>();
            var uiServices = Container.GetExportedValue<IUIServices>();
            uiServices.Initialize();

            window.Show();

            if (e.Args.Length > 0)
            {
                var file = e.Args[0];
                var successful = await LoadFile(window, file);
                if (successful)
                {
                    return;
                }
            }
        }

        private void InitCredentialService()
        {
            Task<IEnumerable<ICredentialProvider>> getProviders()
            {
                return Task.FromResult<IEnumerable<ICredentialProvider>>(new ICredentialProvider[]
                {
                    Container.GetExportedValue<CredentialManagerProvider>(),
                    Container.GetExportedValue<CredentialPublishProvider>(),
                    Container.GetExportedValue<CredentialDialogProvider>()
                });
            };

            HttpHandlerResourceV3.CredentialService =
                new Lazy<ICredentialService>(() => new CredentialService(
                                                      new AsyncLazy<IEnumerable<ICredentialProvider>>(() => getProviders()),
                                                      nonInteractive: false,
                                                      handlesDefaultCredentials: false));

        }

        private static void MigrateSettings()
        {
            try
            {
                var settings = Settings.Default;
                if (settings.IsFirstTime)
                {
                    settings.Upgrade();
                    settings.IsFirstTime = false;
                    settings.Save();
                }
            }
            catch { }
        }

        private static async Task<bool> LoadFile(MainWindow window, string file)
        {
            if (FileUtility.IsSupportedFile(file) && File.Exists(file))
            {
                await window.OpenLocalPackage(file);
                return true;
            }
            else
            {
                return false;
            }
        }

        private void Application_Exit(object sender, ExitEventArgs e)
        {
            if (_container != null)
            {
                _container.Dispose();
            }

            // Try to save, if there's an IO error, just ignore it here, nothing we can do
            try
            {
                Settings.Default.Save();
            }
            catch
            {
            }

            DiagnosticsClient.TrackEvent("AppExit");

            DiagnosticsClient.OnExit();
        }

        private void PackageIconImage_ImageFailed(object sender, ExceptionRoutedEventArgs e)
        {
            if (sender is Image image)
            {
                image.Source = Images.DefaultPackageIcon;
            }
        }
    }
}
