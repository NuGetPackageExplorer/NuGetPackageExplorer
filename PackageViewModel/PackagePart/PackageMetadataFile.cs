using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Runtime.Versioning;

using NuGet.Frameworks;

namespace PackageExplorerViewModel
{
    internal class PackageMetadataFile : IEditablePackageFile
    {
        private readonly PackageViewModel _packageViewModel;

        public PackageMetadataFile(string name, string filePath, PackageViewModel packageViewModel)
        {
            Debug.Assert(name != null);
            Debug.Assert(filePath != null);
            Debug.Assert(packageViewModel != null);

            OriginalPath = filePath;
            EffectivePath = name;
            _packageViewModel = packageViewModel;
        }

        public string? OriginalPath { get; }

        public string Name
        {
            get { return EffectivePath; }
        }

        public string Path
        {
            get { return EffectivePath; }
        }

        public Stream GetStream()
        {
            return File.OpenRead(OriginalPath!);
        }

        public bool Save(string editedFilePath)
        {
            return _packageViewModel.SaveMetadataAfterEditSource(editedFilePath);
        }

        public string EffectivePath { get; }

        public FrameworkName? TargetFramework
        {
            get { return null; }
        }

#pragma warning disable CA1822 // Mark members as static
        public IEnumerable<FrameworkName> SupportedFrameworks
#pragma warning restore CA1822 // Mark members as static
        {
            get { return Array.Empty<FrameworkName>(); }
        }

        public DateTimeOffset LastWriteTime => DateTimeOffset.MinValue;

        public NuGetFramework? NuGetFramework => null;
    }
}
