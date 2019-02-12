using System;
using System.IO;
using System.Runtime.Versioning;

namespace NuGetPackageExplorer.Types
{
    public interface IPackageContent
    {
        DateTimeOffset LastWriteTime { get; }
        string? OriginalPath { get; }
        string Name { get; }
        string? Extension { get; }
        string Path { get; }
        Stream GetStream();
    }
}
