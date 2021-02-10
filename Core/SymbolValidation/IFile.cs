using System.Collections.Generic;
using System.IO;
using System.Runtime.Versioning;

using NuGetPe.AssemblyMetadata;

namespace NuGetPe
{
    public interface IFile : IPart
    {
        IEnumerable<IFile> GetAssociatedFiles();

        AssemblyDebugData? DebugData { get; set; }

        Stream GetStream();
        FrameworkName TargetFramework { get; }
        string? Extension { get; }
    }
}
