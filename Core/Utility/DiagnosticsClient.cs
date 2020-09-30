using System;
using System.Collections.Generic;
using System.Windows;
using Microsoft.ApplicationInsights;
using Microsoft.ApplicationInsights.Extensibility;
using NuGetPe.Utility;

namespace NuGetPe
{
    public static class DiagnosticsClient
    {
        private static TelemetryClient?  _client;

        public static void Initialize(bool forLibrary = false)
        {
#pragma warning disable CA2000 // Dispose objects before losing scope
            var config = TelemetryConfiguration.CreateDefault();
#pragma warning restore CA2000 // Dispose objects before losing scope

            if(!forLibrary)
            {
                config.TelemetryInitializers.Add(new AppVersionTelemetryInitializer());
                config.TelemetryInitializers.Add(new EnvironmentTelemetryInitializer());

                Application.Current.DispatcherUnhandledException += App_DispatcherUnhandledException;
            }
            
            _client = new TelemetryClient(config);
        }

        public static void OnExit()
        {
            if (_client == null) return;
            _client.Flush();

            // Allow time for flushing and sending:
            System.Threading.Thread.Sleep(2000);
        }

        private static void App_DispatcherUnhandledException(object sender, System.Windows.Threading.DispatcherUnhandledExceptionEventArgs e)
        {
            TrackException(e.Exception);
        }

        public static void TrackEvent(string eventName, IDictionary<string, string>? properties = null, IDictionary<string, double>? metrics = null)
        {
            if (_client == null) return;
            _client.TrackEvent(eventName, properties, metrics);
        }

        public static void TrackEvent(string eventName, IPackage package, bool packageIsPublic, IDictionary<string, string>? properties = null, IDictionary<string, double>? metrics = null)
        {
            if (_client == null) return;

            if(packageIsPublic && package != null)
            {
                properties??= new Dictionary<string, string>();

                properties.Add("packageId", package.Id);
                properties.Add("packageVersion", package.Version.ToNormalizedString());
            }

            _client.TrackEvent(eventName, properties, metrics);
        }

        public static void TrackTrace(string evt, IDictionary<string, string>? properties = null)
        {
            if (_client == null) return;
            _client.TrackTrace(evt, properties);
        }

        public static void TrackException(Exception exception, IDictionary<string, string>? properties = null, IDictionary<string, double>? metrics = null)
        {
            if (_client == null) return;
            _client.TrackException(exception, properties, metrics);
        }

        public static void TrackException(Exception exception, IPackage package, bool packageIsPublic, IDictionary<string, string>? properties = null, IDictionary<string, double>? metrics = null)
        {
            if (_client == null) return;


            if (packageIsPublic && package != null)
            {
                properties ??= new Dictionary<string, string>();

                properties.Add("packageId", package.Id);
                properties.Add("packageVersion", package.Version.ToNormalizedString());
            }

            _client.TrackException(exception, properties, metrics);
        }

        public static void TrackPageView(string pageName)
        {
            if (_client == null) return;
            _client.TrackPageView(pageName);
        }
    }
}
