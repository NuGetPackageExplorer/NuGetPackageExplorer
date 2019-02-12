using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Runtime.Versioning;

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
            return File.OpenRead(OriginalPath);
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

        public IEnumerable<FrameworkName> SupportedFrameworks
        {
            get { return new FrameworkName[0]; }
        }

        public DateTimeOffset LastWriteTime => DateTimeOffset.MinValue;
    }
}
