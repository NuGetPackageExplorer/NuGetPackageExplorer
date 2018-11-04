using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NuGetPe.AssemblyMetadata
{
    public class AssemblyDebugData
    {
        public IReadOnlyList<SourceLinkMap> SourceLink { get; set; }

        public IReadOnlyList<AssemblyDebugSourceDocument> Sources { get; internal set; }
    }
}
