#if __WASM__
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using Microsoft.ApplicationInsights.Extensibility;
using Microsoft.Extensions.Logging;

using NuGetPe;

using Uno.Extensions;
using Uno.Logging;

namespace NuGetPackageExplorer.Services
{
    public class AppInsightsJsTelemetryService : ITelemetryService
    {
        private static readonly ILogger _logger = typeof(AppInsightsJsTelemetryService).Log();

        private readonly bool _initialized;
        private readonly List<ITelemetryServiceInitializer> _initializers;

        public AppInsightsJsTelemetryService(List<ITelemetryServiceInitializer> initializers)
        {
            _initialized = GetIsInitialized();
            _initializers = initializers;

            if (_initialized && _logger.IsEnabled(LogLevel.Debug))
            {
                _logger.Debug("App Insights SDK initialized successfully");
            }
            if (!_initialized && _logger.IsEnabled(LogLevel.Error))
            {
                _logger.Error("App Insights SDK failed to initialize");
            }
        }

        private bool GetIsInitialized()
        {
            var result = InvokeJS("appInsights && !!appInsights.core");

            return bool.TryParse(result, out var parsed) ? parsed : false;
        }

        public void TrackEvent(string eventName, IDictionary<string, string>? properties, IDictionary<string, double>? metrics)
        {
            if (!_initialized) return;

            var localProperties = BuildLocalProperties(properties);

            _logger.DebugIfEnabled(() => $"TrackEvent: {eventName}");

            InvokeJS("appInsights.trackEvent({\n" +
                $"  name: \"{EscapeJs(eventName)}\",\n" +
                $"  properties: {ToJsObject(localProperties)}\n" +
            "\n});");
        }

        public void TrackException(Exception exception, IDictionary<string, string>? properties, IDictionary<string, double>? metrics)
        {
            if (!_initialized) return;
            
            _logger.DebugIfEnabled(() => $"TrackException: {exception}");

            var localProperties = BuildLocalProperties(properties);

            InvokeJS("appInsights.trackException({\n" +
                $"  exception: new Error(\"{EscapeJs(exception.Message)}\"),\n" +
                $"  properties: {ToJsObject(localProperties)}\n" +
            "\n});");
        }

        public void TrackPageView(string pageName)
        {
            if (!_initialized) return;
            
            _logger.DebugIfEnabled(() => $"TrackPageView: {pageName}");

            var localProperties = BuildLocalProperties(null);

            InvokeJS("appInsights.trackPageView({\n" +
                $"  name: \"{EscapeJs(pageName)}\",\n" +
                $"  properties: {ToJsObject(localProperties)}\n" +
            "\n});");
        }

        public void TrackTrace(string evt, IDictionary<string, string>? properties)
        {
            if (!_initialized) return;

            _logger.DebugIfEnabled(() => $"TrackTrace: {evt}");

            var localProperties = BuildLocalProperties(properties);

            InvokeJS("appInsights.trackTrace({\n" +
                $"  message: \"{EscapeJs(evt)}\",\n" +
                $"  properties: {ToJsObject(localProperties)}\n" +
            "\n});");
        }

        public void Flush()
        {
            if (!_initialized) return;

            _logger.DebugIfEnabled(() => $"Flush");
            InvokeJS($"appInsights.flush();");
        }

        private static string? ToJsObject(IDictionary<string, string> o)
        {
            if (o == null) return null;
            if (o.Count == 0) return "{}";

            return "{" + string.Join(", ", o.Select(x => $"\"{x.Key}\": {FormatValue(x.Value)}")) + "}";

            string FormatValue(string x) => x != null
                ? ('"' + EscapeJs(x) + '"')
                : "null";
        }

        private static string EscapeJs(string value) => Uno.Foundation.WebAssemblyRuntime.EscapeJs(value);
        private static string InvokeJS(string js)
        {
            if (_logger.IsEnabled(LogLevel.Trace))
            {
                _logger.LogTrace(string.Join("\n",
                    "Invoking JS:",
                    "```",
                    js,
                    "```"
                ));
            }
            
            return Uno.Foundation.WebAssemblyRuntime.InvokeJS(js);
        }

        private Dictionary<string, string> BuildLocalProperties(IDictionary<string, string>? properties)
        {
            var props = properties != null ? new Dictionary<string, string>(properties) : new Dictionary<string, string>();

            foreach (var initializer in _initializers)
            {
                foreach (var prop in initializer.Properties)
                {
                    props.Add(prop.Key, prop.Value);
                }
            }

            return props;
        }
    }
}
#endif
