using System;
using System.IO;

namespace NuGetPe
{
    internal static class FileSystemExtensions
    {
        internal static void AddFile(this IFileSystem fileSystem, string path, Action<Stream> write)
        {
            using var stream = new MemoryStream();
            write(stream);
            stream.Seek(0, SeekOrigin.Begin);
            fileSystem.AddFile(path, stream);
        }
    }
}