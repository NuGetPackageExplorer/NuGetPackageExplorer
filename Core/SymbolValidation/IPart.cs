using System.Collections.Generic;

namespace NuGetPe
{
    public interface IPart
    {
        string Path { get; }

        string Name { get; }

        IFolder? Parent { get; }

        IEnumerable<IFile> GetFiles();
    }
}
