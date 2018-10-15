using System.Collections.Generic;
using PackageExplorerViewModel;

namespace NuGetPackageExplorer.Types
{
    public sealed class FileContentInfo
    {
        public FileContentInfo(PackageFile file, string name, object content, bool isTextFile, long size, IReadOnlyList<AuthenticodeSignature> signatures)
        {
            File = file;
            Name = name;
            Content = content;
            IsTextFile = isTextFile;
            Size = size;
            Signatures = signatures;
        }

        public PackageFile File { get; private set; }
        public string Name { get; private set; }
        public object Content { get; private set; }
        public bool IsTextFile { get; private set; }
        public long Size { get; private set; }
        public IReadOnlyList<AuthenticodeSignature> Signatures { get; }
    }
}
