using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using NuGet.Packaging;

namespace PackageExplorerViewModel.Utilities
{
    public class TemporaryFile : IDisposable
    {
        public TemporaryFile(Stream stream, string extension)
        {
            if (stream == null)
                throw new ArgumentNullException(nameof(stream));

            if (string.IsNullOrWhiteSpace(extension) || extension[0] != '.')
            {
                extension = string.Empty;
            }

            FileName = Path.GetTempFileName() + extension;

            stream.CopyToFile(FileName);
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
