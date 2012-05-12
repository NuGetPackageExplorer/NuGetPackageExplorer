using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.ComponentModel.Composition.Hosting;
using System.Deployment.Application;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Windows;
using NuGet;
using PackageExplorer.Properties;
using PackageExplorerViewModel;

namespace PackageExplorer
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
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

        private void Application_Startup(object sender, StartupEventArgs e)
        {
            MigrateSettings();

            HttpClient.DefaultCredentialProvider = new WindowsCredentialProvider();

            var window = Container.GetExportedValue<MainWindow>();
            window.Show();

            if (e.Args.Length > 0)
            {
                string file = e.Args[0];
                bool successful = LoadFile(window, file);
                if (successful)
                {
                    return;
                }
            }

            if (AppDomain.CurrentDomain.SetupInformation.ActivationArguments != null)
            {
                // click-once deployment
                string[] activationData = AppDomain.CurrentDomain.SetupInformation.ActivationArguments.ActivationData;
                if (activationData != null && activationData.Length > 0)
                {
                    string file = activationData[0];
                    LoadFile(window, file);
                    return;
                }
            }

            if (ApplicationDeployment.IsNetworkDeployed)
            {
                // click-once with command in the URL
                ProcessUrlParameters(window);
            }
        }

        private static void ProcessUrlParameters(MainWindow window)
        {
            const string ActionParameter = "action";
            const string LoadPackageAction = "load-package";
            const string PackageSourceParameter = "packagesource";
            const string PackageUrlParameter = "packageurl";

            Dictionary<string, string> requestParameters =
                ApplicationDeployment.CurrentDeployment.ActivationUri.GetRequestParameters();

            if (requestParameters != null && requestParameters.ContainsKey(ActionParameter))
            {
                switch (requestParameters[ActionParameter].ToLowerInvariant())
                {
                    case LoadPackageAction:
                        string packageSourceValue;
                        if (requestParameters.TryGetValue(PackageSourceParameter, out packageSourceValue))
                        {
                            window.SetActivePackagePublishSource(packageSourceValue);
                        }

                        string packageUrlValue;
                        if (requestParameters.TryGetValue(PackageUrlParameter, out packageUrlValue))
                        {
                            window.DownloadAndOpenDataServicePackage(packageUrlValue);
                        }
                        break;
                }
            }
        }

        private void MigrateSettings()
        {
            Settings settings = Settings.Default;
            if (settings.IsFirstTime)
            {
                settings.Upgrade();
                settings.Save();
            }
        }

        private static bool LoadFile(MainWindow window, string file)
        {
            if (FileUtility.IsSupportedFile(file) && File.Exists(file))
            {
                window.OpenLocalPackage(file);
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
            Settings.Default.IsFirstTime = false;
            Settings.Default.Save();
        }
    }
}