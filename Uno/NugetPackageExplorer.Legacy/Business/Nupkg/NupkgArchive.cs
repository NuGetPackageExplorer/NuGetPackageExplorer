using System.IO.Compression;
using System.Xml.Linq;

using NupkgExplorer.Business.Nuspec;
using NupkgExplorer.Extensions;

using Uno.Logging;

namespace NupkgExplorer.Business.Nupkg
{
    public class NupkgArchive
    {
        private readonly Stream _stream;
        private readonly ZipArchive _nupkg;

        public NupkgArchive(Stream stream)
        {
            _stream = stream;
            _nupkg = new ZipArchive(stream);
        }

        public NuspecMetadata? GetMetadata()
        {
            try
            {
                var nuspec = _nupkg.Entries
                    .FirstOrDefault(static x => x.Name == x.FullName && x.Name.EndsWith(".nuspec", StringComparison.InvariantCultureIgnoreCase))
                    ?? throw new FileNotFoundException("Unable to find the nuspec file.");
                var document = XDocument.Load(nuspec.Open());

                return NuspecMetadata.Parse(document);
            }
            catch (Exception e)
            {
                this.Log().Error("Failed to read metadata", e);
                return null;
            }
        }

        public IEnumerable<NupkgContentFile> GetContentFiles()
        {
            return _nupkg.Entries.Select(static x => new NupkgContentFile(x));
        }

        public INupkgFileSystemObject[] GetHierarchicalContentFiles()
        {
            var directories = _nupkg.Entries
                .OrderBy(static x => x.FullName.Replace(Path.AltDirectorySeparatorChar, Path.DirectorySeparatorChar))
                .Select(static x => new NupkgContentFile(x))
                .GroupBy(static x => Path.GetDirectoryName(x.FullName), static (directory, files) => new NupkgContentDirectory(directory!, [.. files]))
                .ToArray();
            var mapping = directories.ToDictionary(static x => x.FullName);
            var virtualRoot = mapping.GetOrAddValue(string.Empty, static k => new NupkgContentDirectory(k));
            var queue = new Queue<NupkgContentDirectory>(directories);

            // rebuild hierarchy
            while (queue.Count > 0 && queue.Dequeue() is NupkgContentDirectory current)
            {
                if (string.IsNullOrEmpty(current.FullName)) continue;

                // find or create parent directory
                var parentName = Path.GetDirectoryName(current.FullName)!;
                if (!mapping.TryGetOrAddValue(parentName, static k => new NupkgContentDirectory(k!), out var parent))
                {
                    queue.Enqueue(parent);
                }

                parent.Items.Add(current);
            }

            return virtualRoot.Items
                .OrderByDescending(static x => x is NupkgContentDirectory) // sort directory first
                .ToArray();
        }
    }
}
