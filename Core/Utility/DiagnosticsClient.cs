using System;
using System.Collections.Generic;
using System.Reflection;
using Microsoft.ApplicationInsights;
using Microsoft.ApplicationInsights.Extensibility;
using NuGetPe.Utility;

namespace NuGetPe
{
    public static class DiagnosticsClient
    {
        private static TelemetryClient?  _client;

        public static void Initialize(Assembly wpfAssembly)
        {
#pragma warning disable CA2000 // Dispose objects before losing scope
            var config = TelemetryConfiguration.CreateDefault();
#pragma warning restore CA2000 // Dispose objects before losing scope

            config.TelemetryInitializers.Add(new AppVersionTelemetryInitializer(wpfAssembly));
            config.TelemetryInitializers.Add(new EnvironmentTelemetryInitializer());

            _client = new TelemetryClient(config);
        }

        public static void OnExit()
        {
            if (_client == null) return;
            _client.Flush();

            // Allow time for flushing and sending:
            System.Threading.Thread.Sleep(2000);
        }

        public static void TrackEvent(string eventName, IDictionary<string, string>? properties = null, IDictionary<string, double>? metrics = null)
        {
            _client?.TrackEvent(eventName, properties, metrics);
        }

        public static void TrackTrace(string evt, IDictionary<string, string>? properties = null)
        {
            _client?.TrackTrace(evt, properties);
        }

        public static void TrackException(Exception exception, IDictionary<string, string>? properties = null, IDictionary<string, double>? metrics = null)
        {
            _client?.TrackException(exception, properties, metrics);
        }

        public static void TrackPageView(string pageName)
        {
            _client?.TrackPageView(pageName);
        }
    }
}
