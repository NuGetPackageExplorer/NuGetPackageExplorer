using System;
using System.ComponentModel.Composition;
using System.ComponentModel.Composition.Hosting;
using System.IO;
using System.Windows;
using PackageExplorerViewModel;
using NuGetPackageExplorer.Types;

namespace PackageExplorer {
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application {

        private CompositionContainer _container;

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Reliability", "CA2000:Dispose objects before losing scope")]
        internal CompositionContainer Container {
            get {
                if (_container == null) {
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

        private void Application_Startup(object sender, StartupEventArgs e) {
            MigrateSettings();

            MainWindow window = Container.GetExportedValue<MainWindow>();
            window.Show();

            if (e.Args.Length > 0) {
                string file = e.Args[0];
                bool successful = LoadFile(window, file);
                if (successful) {
                    return;
                }
            }

            if (AppDomain.CurrentDomain.SetupInformation != null &&
                AppDomain.CurrentDomain.SetupInformation.ActivationArguments != null) {
                // click-once deployment
                var activationData = AppDomain.CurrentDomain.SetupInformation.ActivationArguments.ActivationData;
                if (activationData != null && activationData.Length > 0) {
                    string file = activationData[0];
                    LoadFile(window, file);
                }
            }
        }

        private void MigrateSettings() {
            var settings = PackageExplorer.Properties.Settings.Default;
            if (settings.IsFirstTime) {
                settings.Upgrade();
                settings.IsFirstTime = false;
                settings.Save();
            }
        }

        private static bool LoadFile(MainWindow window, string file) {
            if (FileUtility.IsSupportedFile(file) && File.Exists(file)) {
                window.OpenLocalPackage(file);
                return true;
            }
            else {
                return false;
            }
        }

        private void Application_Exit(object sender, ExitEventArgs e) {
            PackageExplorer.Properties.Settings.Default.Save();
            if (_container != null) {
                _container.Dispose();
            }
        }
    }
}
