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
    class SessionTelemetry : ITelemetryInitializer
    {
        private readonly string? _userName;
        private readonly string _operatingSystem = RuntimeInformation.OSDescription?.Replace("Microsoft ", ""); // Shorter description
        private readonly string _session = Guid.NewGuid().ToString();

#if STORE
        private const string Channel = "store";
#elif NIGHTLY
        private const string Channel = "nightly";
#elif CHOCO
        private const string Channel = "chocolatey";
#else
        private const string Channel = "zip";
#endif


        public SessionTelemetry()
        {
            try
            {
                using (var hash = SHA256.Create())
                {
                    var hashBytes = hash.ComputeHash(Encoding.UTF8.GetBytes(Environment.MachineName + Environment.UserDomainName + Environment.UserName));
                    _userName = Convert.ToBase64String(hashBytes);
                }
            }
            catch
            {
                // No user id                
            }
        }

        public void Initialize(ITelemetry telemetry)
        {
            telemetry.Context.GlobalProperties["Environment"] = Channel;
            // Always default to development if we're in the debugger
            if (Debugger.IsAttached)
            {
                telemetry.Context.GlobalProperties["Environment"] = "development";
            }

            if (_userName != null)
            {
                telemetry.Context.User.Id = _userName;
            }

            telemetry.Context.Session.Id = _session;
            telemetry.Context.Device.OperatingSystem = _operatingSystem;            
        }
    }
}
