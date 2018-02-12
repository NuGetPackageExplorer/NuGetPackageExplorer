using System;
using System.ComponentModel.Composition;
using System.Diagnostics.CodeAnalysis;

namespace NuGetPackageExplorer.Types
{
    [MetadataAttribute]
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Property | AttributeTargets.Field, AllowMultiple = false)]
    public sealed class PackageContentViewerMetadataAttribute : ExportAttribute
    {
        private readonly int _priority;
        private readonly string[] _supportedExtensions;

        public PackageContentViewerMetadataAttribute(int priority, params string[] supportedExtensions) :
            base(typeof(IPackageContentViewer))
        {
            _supportedExtensions = supportedExtensions;
            _priority = priority;
        }

        [SuppressMessage("Microsoft.Performance", "CA1819:PropertiesShouldNotReturnArrays")]
        public string[] SupportedExtensions
        {
            get { return _supportedExtensions; }
        }

        public int Priority
        {
            get { return _priority; }
        }

        public bool SupportsWindows10S { get; set; } = true;
    }
}
