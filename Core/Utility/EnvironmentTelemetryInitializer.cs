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
    internal class EnvironmentTelemetryInitializer : ITelemetryInitializer
    {

#if STORE
        private const string Channel = "store";
#elif NIGHTLY
        private const string Channel = "nightly";
#elif CHOCO
        private const string Channel = "chocolatey";
#else
        private const string Channel = "zip";
#endif

        public void Initialize(ITelemetry telemetry)
        {
            telemetry.Context.GlobalProperties["Environment"] = Channel;
            // Always default to development if we're in the debugger
            if (Debugger.IsAttached)
            {
                telemetry.Context.GlobalProperties["Environment"] = "development";
            }       
        }
    }
}
