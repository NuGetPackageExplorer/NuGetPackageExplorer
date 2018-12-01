using System.Collections.Generic;
using System.IO;
using NuGet.Packaging;

namespace NuGetPackageExplorer.Types
{
    public interface IPackageContentViewer
    {
        object GetView(IPackageContent selectedFile, IReadOnlyList<IPackageContent> peerFiles);
    }
}
