using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NuGetPe.AssemblyMetadata
{
#pragma warning disable CS8618 // Non-nullable field is uninitialized.
    public class AssemblyDebugData
#pragma warning restore CS8618 // Non-nullable field is uninitialized.
    {
        public PdbType PdbType { get; internal set; }

        public IReadOnlyList<SourceLinkMap> SourceLink { get; internal set; }

        public IReadOnlyList<AssemblyDebugSourceDocument> Sources { get; internal set; }
    }

    public enum PdbType
    {
        Portable,
        Embedded,
        Full
    }
}
