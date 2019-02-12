using System.Diagnostics;

namespace NuGetPe.AssemblyMetadata
{

#pragma warning disable CS8618 // Non-nullable field is uninitialized.
    [DebuggerDisplay("{Base} => {Location}")]
    public class SourceLinkMap
    {
        public string Base { get; internal set; }

        public string Location { get; internal set; }
    }
#pragma warning restore CS8618 // Non-nullable field is uninitialized.
}
