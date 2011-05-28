using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.IO;

namespace NuGet {
    public interface IPackage : IPackageMetadata, IServerPackageMetadata {
        IEnumerable<IPackageAssemblyReference> AssemblyReferences { get; }

        [SuppressMessage("Microsoft.Design", "CA1024:UsePropertiesWhereAppropriate", Justification = "This might be expensive")]
        IEnumerable<IPackageFile> GetFiles();

        [SuppressMessage("Microsoft.Design", "CA1024:UsePropertiesWhereAppropriate", Justification = "This might be expensive")]
        Stream GetStream();
    }
}
