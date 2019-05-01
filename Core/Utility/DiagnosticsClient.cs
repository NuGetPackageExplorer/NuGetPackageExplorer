using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using Microsoft.ApplicationInsights;
using Microsoft.ApplicationInsights.Extensibility;
using NuGetPe.Utility;

namespace NuGetPe
{
    public static class DiagnosticsClient
    {
        private static bool _initialized;

        private static TelemetryClient  _client;



        public static void Initialize()
        {
            TelemetryConfiguration.Active.TelemetryInitializers.Add(new AppVersionTelemetryInitializer());
            TelemetryConfiguration.Active.TelemetryInitializers.Add(new EnvironmentTelemetryInitializer());
            
            Application.Current.DispatcherUnhandledException += App_DispatcherUnhandledException;

            _initialized = true;

            _client = new TelemetryClient();
        }

        public static void OnExit()
        {
            if (!_initialized) return;

            _client.Flush();

            // Allow time for flushing and sending:
            System.Threading.Thread.Sleep(1000);
        }

        private static void App_DispatcherUnhandledException(object sender, System.Windows.Threading.DispatcherUnhandledExceptionEventArgs e)
        {
            TrackException(e.Exception);
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

        public static void TrackException(Exception exception)
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
