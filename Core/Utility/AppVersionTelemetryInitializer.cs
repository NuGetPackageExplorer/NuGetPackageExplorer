using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using Microsoft.ApplicationInsights.Channel;
using Microsoft.ApplicationInsights.Extensibility;

namespace NuGetPe.Utility
{
    internal class AppVersionTelemetryInitializer : ITelemetryInitializer
    {
#if WINDOWS
        private readonly string _wpfVersion;
#endif
        private readonly string _appVersion;

        public AppVersionTelemetryInitializer()
        {
#if WINDOWS
            _wpfVersion = typeof(System.Windows.Application).Assembly.GetCustomAttribute<AssemblyInformationalVersionAttribute>()!.InformationalVersion;
#endif
            _appVersion = typeof(DiagnosticsClient).Assembly.GetCustomAttributes<AssemblyMetadataAttribute>()
                                                            .First(ama => string.Equals(ama.Key, "CloudBuildNumber", StringComparison.OrdinalIgnoreCase))
                                                            .Value!;
        }

        public void Initialize(ITelemetry telemetry)
        {
#if WINDOWS
            telemetry.Context.GlobalProperties["WPF version"] = _wpfVersion;
#endif
            telemetry.Context.Component.Version = _appVersion;
        }
    }

}
