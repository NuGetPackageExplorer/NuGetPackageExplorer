using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
using System.Text;
using Microsoft.ApplicationInsights.Channel;
using Microsoft.ApplicationInsights.Extensibility;
namespace NuGetPe.Utility
{
    public class EnvironmentTelemetryInitializer : ITelemetryInitializer, ITelemetryServiceInitializer
    {
        private Dictionary<string, string> _properties = new Dictionary<string, string>();

#if STORE
        private readonly string _channel = "store";
#elif NIGHTLY
        private readonly string _channel = "nightly";
#elif CHOCO
        private readonly string _channel = "chocolatey";
#else
        private readonly string _channel = AppCompat.IsWasm ? "WebAssembly" : "zip";
#endif

        public IReadOnlyDictionary<string, string> Properties => _properties;

        public EnvironmentTelemetryInitializer()
        {
            _properties["Environment"] = _channel;

            // Always default to development if we're in the debugger
            if (Debugger.IsAttached)
            {
                _properties["Environment"] = "development";
            }
        }

        public void Initialize(ITelemetry telemetry)
        {
            foreach (var item in _properties)
            {
                telemetry.Context.GlobalProperties.Add(item);
            }
        }
    }
}
