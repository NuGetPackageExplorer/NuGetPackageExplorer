using System;
using System.ComponentModel.Composition;
using System.ComponentModel.Composition.Hosting;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Threading.Tasks;
using System.Windows;
using CodeExecutor;
using NuGetPackageExplorer.Types;
using PackageExplorer.Properties;
using PackageExplorerViewModel;

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
            MigrateSettings();

            var window = Container.GetExportedValue<MainWindow>();
            window.Show();

            //Disable this sellup as it doesn't work well with Windows 10 and the Windows Store application is not maintained.
            //CheckWindows8AndDisplayUpsellDialog();

            if (e.Args.Length > 0)
            {
                string file = e.Args[0];
                bool successful = await LoadFile(window, file);
                if (successful)
                {
                    return;
                }
            }
           

            if (AppDomain.CurrentDomain.SetupInformation != null &&
                AppDomain.CurrentDomain.SetupInformation.ActivationArguments != null)
            {
                // click-once deployment
                string[] activationData = AppDomain.CurrentDomain.SetupInformation.ActivationArguments.ActivationData;
                if (activationData != null && activationData.Length > 0)
                {
                    string file = activationData[0];
                    await LoadFile(window, file);
                    return;
                }
            }
        }

        private void CheckWindows8AndDisplayUpsellDialog()
        {
            if (NativeMethods.IsWindows8OrLater && Settings.Default.SolicitInstallNpeForWin8)
            {
                bool isInstalled = RemoteCodeExecutor.IsNpeMetroInstalled;
                if (!isInstalled)
                {
                    IUIServices uiServices = Container.GetExportedValue<IUIServices>();
                    bool? result = uiServices.AskToInstallNpeOnWindows8();

                    // if result == null, remind user next time
                    Settings.Default.SolicitInstallNpeForWin8 = (result != false);

                    if (result == true)
                    {
                        Process.Start("ms-windows-store:PDP?PFN=50582LuanNguyen.NuGetPackageExplorer_w6y2tyx5bpzwa");
                    }
                }
            }
        }

        private static void MigrateSettings()
        {
            Settings settings = Settings.Default;
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
            Settings.Default.Save();
        }
    }
}