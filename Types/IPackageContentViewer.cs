using System.IO;

namespace NuGetPackageExplorer.Types
{
    public interface IPackageContentViewer
    {
        object GetView(string extension, Stream stream);
    }
}