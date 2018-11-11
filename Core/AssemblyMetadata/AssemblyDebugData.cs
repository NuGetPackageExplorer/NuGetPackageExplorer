using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NuGetPe.AssemblyMetadata
{
    public class AssemblyDebugData
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
