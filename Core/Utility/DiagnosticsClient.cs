using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using Bugsnag;
using Bugsnag.Payload;
using Exception = System.Exception;

namespace NuGetPe
{
    public static class DiagnosticsClient
    {
        private static bool _initialized;
        private static Client _client;

#if STORE
        private const string Channel = "store";
#elif NIGHTLY
        private const string Channel = "nightly";
#elif CHOCO
        private const string Channel = "chocolatey";
#else
        private const string Channel = "zip";
#endif


        public static void Initialize(string apiKey, string sourceRoots)
        {
            _initialized = true;

            var config = new Configuration(apiKey)
            {
                AutoCaptureSessions = true,
                AutoNotify = true,
                NotifyReleaseStages = new[] { "development", "store", "nightly", "chocolatey", "zip" },
                ProjectNamespaces = new[] { "NuGetPe", "PackageExplorer", "PackageExplorerViewModel", "NuGetPackageExplorer.Types" },
                ReleaseStage = Channel,
                ProjectRoots = sourceRoots.Split(';'),
                AppVersion = typeof(DiagnosticsClient).Assembly
                                                      .GetCustomAttributes<AssemblyMetadataAttribute>()
                                                      .FirstOrDefault(ama => string.Equals(ama.Key, "CloudBuildNumber", StringComparison.OrdinalIgnoreCase))
                                                      ?.Value,
                

            };

            // Always default to development if we're in the debugger
            if (Debugger.IsAttached)
            {
                config.ReleaseStage = "development";
            }

            var infoVersion = typeof(System.Windows.Application).Assembly.GetCustomAttribute<AssemblyInformationalVersionAttribute>().InformationalVersion;
            var corelibinfoVersion = typeof(string).Assembly.GetCustomAttribute<AssemblyInformationalVersionAttribute>().InformationalVersion;

            _client = new Client(config);

            _client.SessionTracking.CreateSession();

            // Force it to send right away, don't wait 60 seconds as that will lose a lot of data
            try
            {
                var sendSessions = typeof(SessionsStore).GetMethod("SendSessions", BindingFlags.Instance | BindingFlags.NonPublic);
                sendSessions.Invoke(SessionsStore.Instance, new object[] { null });
            }
            catch
            {
            }

            _client.BeforeNotify(cb =>
            {
                cb.Event.Device.Remove("hostname");
                cb.Event.Device.AddToPayload("presentationFramework", infoVersion);
                cb.Event.Device.AddToPayload("coreClr", corelibinfoVersion);                
            });
        }

        public static void Notify(Exception exception, Severity severity = Severity.Error)
        {
            if (!_initialized) return;

            _client.Notify(exception);
        }

        public static void Notify(Exception exception, Middleware callback)
        {
            if (!_initialized) return;

            _client.Notify(exception, callback);
        }

        public static void Breadcrumb(string message)
        {
            if (!_initialized) return;

            _client.Breadcrumbs.Leave(message);
        }

        public static void Breadcrumb(string message, BreadcrumbType type, IDictionary<string, string> metadata = null)
        {
            if (!_initialized) return;

            _client.Breadcrumbs.Leave(message, type, metadata);
        }
    }
}
