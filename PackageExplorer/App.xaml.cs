using System.ComponentModel.Composition;
using System.ComponentModel.Composition.Hosting;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using NuGet.Credentials;
using NuGet.Protocol;
using PackageExplorer.Properties;
using PackageExplorerViewModel;
using PackageExplorerViewModel.Types;

namespace PackageExplorer
{
    public partial class App : Application
    {
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
            InitCredentialService();
            HttpHandlerResourceV3.CredentialsSuccessfullyUsed = (uri, credentials) =>
            {
                Container.GetExportedValue<ICredentialManager>().Add(credentials, uri);
                InitCredentialService();
            };

            MigrateSettings();

            var window = Container.GetExportedValue<MainWindow>();
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
            HttpHandlerResourceV3.CredentialService = new CredentialService(new ICredentialProvider[] {
                Container.GetExportedValue<CredentialManagerProvider>(),
                Container.GetExportedValue<CredentialPublishProvider>(),
                Container.GetExportedValue<CredentialDialogProvider>(),
            }, nonInteractive: false);
        }

        private static void MigrateSettings()
        {
            var settings = Settings.Default;
            if (settings.IsFirstTime)
            {
                settings.Upgrade();
                settings.IsFirstTime = false;
                settings.Save();
            }
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

            // IMPORTANT: Call this after calling _container.Dispose(). Some exports relies on Dispose()
            // being called to save settings values.
            Settings.Default.IsFirstTimeAfterMigrate = false;

            // Try to save, if there's an IO error, just ignore it here, nothing we can do
            try
            {
                Settings.Default.Save();
            }
            catch 
            {
            }
        }

        private void PackageIconImage_ImageFailed(object sender, ExceptionRoutedEventArgs e)
        {
            var image = sender as Image;
            image.Source = Images.DefaultPackageIcon;
        }
    }
}
