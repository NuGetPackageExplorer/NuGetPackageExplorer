using System.Diagnostics.CodeAnalysis;

namespace NuGetPackageExplorer.Types
{
    public interface IPackageContentViewerMetadata
    {
        int Priority { get; }

        [SuppressMessage("Microsoft.Performance", "CA1819:PropertiesShouldNotReturnArrays")]
        string[] SupportedExtensions { get; }

        bool SupportsWindows10S { get; }
    }
}
