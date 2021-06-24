using System.Collections.Generic;

namespace NuGetPe
{
    public interface ITelemetryServiceInitializer
    {
        IReadOnlyDictionary<string,string> Properties { get; }
    }
}
