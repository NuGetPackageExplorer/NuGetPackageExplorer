using System;
using System.IO;

namespace NuGetPe
{
    public sealed class TemporaryFile : IDisposable
    {
        public TemporaryFile(Stream stream, string? extension = null)
            : this(GeneratePath(extension), stream)
        {
        }

        public TemporaryFile(string path, Stream stream)
        {
            if (path == null)
                throw new ArgumentNullException(nameof(path));
            if (stream == null)
                throw new ArgumentNullException(nameof(stream));

            var directory = Path.GetDirectoryName(path);
            if (directory is not null && !Directory.Exists(directory))
            {
                Directory.CreateDirectory(directory);
            }

            FileName = path;
            using var fstream = File.Open(FileName, FileMode.Create);
            stream.CopyTo(fstream);
            fstream.Flush();
        }

        private static string GeneratePath(string? extension)
        {
            if (string.IsNullOrWhiteSpace(extension) || extension[0] != '.')
            {
                return Path.GetTempFileName();
            }
            else
            {
                return Path.Combine(Path.GetTempPath(), Path.GetRandomFileName() + extension);
            }
        }

        public string FileName { get; }

        public long Length => new FileInfo(FileName).Length;

        private bool _disposed;

        public void Dispose()
        {
            if (!_disposed)
            {
                _disposed = true;
                try
                {
                    File.Delete(FileName);
                }
                catch // best effort
                {
                }
            }
        }

    }
}
