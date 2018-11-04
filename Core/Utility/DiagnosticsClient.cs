using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Bugsnag;

namespace NuGetPe
{
    public static class DiagnosticsClient
    {
        static bool _initialized;

        static Client _client;

#if STORE
      const string Channel = "store";
#elif NIGHTLY
      const string Channel = "nightly";
#elif CHOCO
      const string Channel = "chocolatey";
#else
        const string Channel = "zip";
#endif


        public static void Initialize(string apiKey)
        {
            _initialized = true;

            var config = new Configuration(apiKey)
            {
                AutoCaptureSessions = true,
                AutoNotify = true,
                NotifyReleaseStages = new[] { "development", "store", "nightly", "chocolatey", "zip" },
                ReleaseStage = Channel
            };


            // Always default to development if we're in the debugger
            if (Debugger.IsAttached)
            {
                config.ReleaseStage = "development";
            }


            _client = new Client(config);
        }

        public static void Notify(Exception exception)
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
