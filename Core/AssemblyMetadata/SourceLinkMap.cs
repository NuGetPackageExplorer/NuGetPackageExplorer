using System.Diagnostics;

namespace NuGetPe.AssemblyMetadata
{
    [DebuggerDisplay("{Base} => {Location}")]
    public class SourceLinkMap
    {
        public string Base { get; internal set; }

        public string Location { get; internal set; }
    }
}
