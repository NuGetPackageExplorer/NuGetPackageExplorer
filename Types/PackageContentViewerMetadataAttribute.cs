using System;
using System.ComponentModel.Composition;
using System.Diagnostics.CodeAnalysis;

namespace NuGetPackageExplorer.Types
{
    [MetadataAttribute]
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Property | AttributeTargets.Field, AllowMultiple = false)]
    public sealed class PackageContentViewerMetadataAttribute : ExportAttribute
    {
        public PackageContentViewerMetadataAttribute(int priority, params string[] supportedExtensions) :
            base(typeof(IPackageContentViewer))
        {
            SupportedExtensions = supportedExtensions;
            Priority = priority;
        }

        [SuppressMessage("Microsoft.Performance", "CA1819:PropertiesShouldNotReturnArrays")]
        public string[] SupportedExtensions { get; }

        public int Priority { get; }

        public bool SupportsWindows10S { get; set; } = true;
    }
}
