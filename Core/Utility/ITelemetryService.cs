using System;
using System.Collections.Generic;

namespace NuGetPe
{
    public interface ITelemetryService
    {
        void Flush();
        void TrackEvent(string eventName, IDictionary<string, string>? properties, IDictionary<string, double>? metrics);
        void TrackException(Exception exception, IDictionary<string, string>? properties, IDictionary<string, double>? metrics);
        void TrackPageView(string pageName);
        void TrackTrace(string evt, IDictionary<string, string>? properties);
    }
}
