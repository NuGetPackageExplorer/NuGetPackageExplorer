using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using Microsoft.ApplicationInsights;
using Microsoft.ApplicationInsights.Extensibility;
using Microsoft.ApplicationInsights.WindowsServer;

namespace NuGetPe
{
    public static class DiagnosticsClient
    {
        private static bool _initialized;

        static TelemetryClient  _client;

#if STORE
        private const string Channel = "store";
#elif NIGHTLY
        private const string Channel = "nightly";
#elif CHOCO
        private const string Channel = "chocolatey";
#else
        private const string Channel = "zip";
#endif


        public static void Initialize(string apiKey)
        {
            if (!string.IsNullOrWhiteSpace(apiKey))
            {
                TelemetryConfiguration.Active.InstrumentationKey = apiKey;
                TelemetryConfiguration.Active.TelemetryChannel.DeveloperMode = Debugger.IsAttached;                

                _initialized = true;

                _client = new TelemetryClient();
                _client.Context.User.Id = Environment.UserName;
                _client.Context.Session.Id = Guid.NewGuid().ToString();
                _client.Context.Device.OperatingSystem = Environment.OSVersion.ToString();
                _client.Context.Component.Version = typeof(DiagnosticsClient).Assembly
                                                          .GetCustomAttributes<AssemblyMetadataAttribute>()
                                                          .FirstOrDefault(ama => string.Equals(ama.Key, "CloudBuildNumber", StringComparison.OrdinalIgnoreCase))
                                                          ?.Value;
                _client.Context.GlobalProperties["Environment"] = Channel;


                var infoVersion = typeof(System.Windows.Application).Assembly.GetCustomAttribute<AssemblyInformationalVersionAttribute>().InformationalVersion;
                var corelibinfoVersion = typeof(string).Assembly.GetCustomAttribute<AssemblyInformationalVersionAttribute>().InformationalVersion;

                _client.Context.GlobalProperties["WpfVersion"] = infoVersion;
                _client.Context.GlobalProperties["ClrVersion"] = corelibinfoVersion;


                // Always default to development if we're in the debugger
                if (Debugger.IsAttached)
                {                    
                    _client.Context.GlobalProperties["Environment"] = "development";         
                }
            }
        }

        public static void OnExit()
        {
            if (!_initialized) return;

            _client.Flush();
            // Allow time for flushing:
            System.Threading.Thread.Sleep(1000);
        }

        public static void TrackEvent(string evt, IDictionary<string, string>? properties = null, IDictionary<string, double>? metrics = null)
        {
            if (!_initialized) return;
            _client.TrackEvent(evt, properties, metrics);
        }

        public static void TrackTrace(string evt)
        {
            if (!_initialized) return;
            _client.TrackTrace(evt);
        }

        public static void Notify(Exception exception)
        {
            if (!_initialized) return;

            _client.TrackException(exception);
        }
    }
}
