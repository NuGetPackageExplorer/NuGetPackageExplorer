
namespace NuGetPackageExplorer.Types {
    public interface IPackageContentViewerMetadata {
        int Priority { get; }
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Performance", "CA1819:PropertiesShouldNotReturnArrays")]
        string[] SupportedExtensions { get; }
    }
}
