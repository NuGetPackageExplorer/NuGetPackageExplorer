using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using Microsoft.ApplicationInsights;
using Microsoft.ApplicationInsights.Extensibility;
using NuGetPe.Utility;

namespace NuGetPe
{
    public static class DiagnosticsClient
    {
        private static bool _initialized;

        private static TelemetryClient  _client;



        public static void Initialize(string apiKey)
        {
            if (!string.IsNullOrWhiteSpace(apiKey))
            {
                TelemetryConfiguration.Active.InstrumentationKey = apiKey;
                TelemetryConfiguration.Active.TelemetryChannel.DeveloperMode = Debugger.IsAttached;
                TelemetryConfiguration.Active.TelemetryInitializers.Add(new AppVersionTelemetryInitializer());
                TelemetryConfiguration.Active.TelemetryInitializers.Add(new EnvironmentTelemetryInitializer());

                _initialized = true;

                _client = new TelemetryClient();
            }
        }

        public static void OnExit()
        {
            if (!_initialized) return;

            _client.Flush();
            // Allow time for flushing:
            System.Threading.Thread.Sleep(1000);
        }

        public static void TrackEvent(string eventName, IDictionary<string, string>? properties = null, IDictionary<string, double>? metrics = null)
        {
            if (!_initialized) return;
            _client.TrackEvent(eventName, properties, metrics);
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

        public static void TrackPageView(string pageName)
        {
            if (!_initialized) return;

            _client.TrackPageView(pageName);
        }
    }
}
