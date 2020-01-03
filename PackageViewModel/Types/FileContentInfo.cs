using System.Collections.Generic;
using AuthenticodeExaminer;
using PackageExplorerViewModel;

namespace NuGetPackageExplorer.Types
{
    public sealed class FileContentInfo
    {
        public FileContentInfo(PackageFile file, string name, object content, bool isTextFile, long size, bool truncated, IReadOnlyList<AuthenticodeSignature> signatures, SignatureCheckResult signatureStatus)
        {
            File = file;
            Name = name;
            Content = content;
            IsTextFile = isTextFile;
            Size = size;
            SignatureStatus = signatureStatus;
            Signatures = signatures;
            Truncated = truncated;
        }

        public PackageFile File { get; private set; }
        public string Name { get; private set; }
        public object Content { get; private set; }
        public bool IsTextFile { get; private set; }
        public long Size { get; private set; }

        public bool Truncated { get; private set; }
        public SignatureCheckResult SignatureStatus { get; }
        public IReadOnlyList<AuthenticodeSignature> Signatures { get; }
    }
}
