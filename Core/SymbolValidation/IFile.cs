using System.Collections.Generic;
using System.IO;

using NuGetPe.AssemblyMetadata;

namespace NuGetPe
{
    public interface IFile : IPart
    {
        IEnumerable<IFile> GetAssociatedFiles();

        AssemblyDebugData? DebugData { get; set; }

        Stream GetStream();
    }
}
