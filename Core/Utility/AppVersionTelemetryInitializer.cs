using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using Microsoft.ApplicationInsights.Channel;
using Microsoft.ApplicationInsights.Extensibility;

namespace NuGetPe.Utility
{
    public class AppVersionTelemetryInitializer : ITelemetryInitializer, ITelemetryServiceInitializer
    {
        private Dictionary<string, string> _properties = new Dictionary<string, string>();

#if WINDOWS
        private readonly string _wpfVersion;
#endif
        private readonly string _appVersion;

        public AppVersionTelemetryInitializer()
        {
#if WINDOWS
            _wpfVersion = typeof(System.Windows.Application).Assembly.GetCustomAttribute<AssemblyInformationalVersionAttribute>()!.InformationalVersion;
            _properties["WPF version"] = _wpfVersion;
#endif
            _appVersion = typeof(DiagnosticsClient).Assembly.GetCustomAttributes<AssemblyMetadataAttribute>()
                                                            .FirstOrDefault(ama => string.Equals(ama.Key, "CloudBuildNumber", StringComparison.OrdinalIgnoreCase))
                                                            .Value ?? "0.0.0.1";

            _properties["Version"] = _appVersion;
        }

        public IReadOnlyDictionary<string, string> Properties => _properties;

        public void Initialize(ITelemetry telemetry)
        {
            telemetry.Context.Component.Version = _appVersion;

            foreach (var item in _properties)
            {
                telemetry.Context.GlobalProperties.Add(item);
            }
        }
    }

}
