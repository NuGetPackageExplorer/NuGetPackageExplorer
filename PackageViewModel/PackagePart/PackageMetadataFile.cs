using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Runtime.Versioning;

namespace PackageExplorerViewModel
{
    internal class PackageMetadataFile : IEditablePackageFile
    {
        private readonly string _filePath;
        private readonly string _name;
        private readonly PackageViewModel _packageViewModel;

        public PackageMetadataFile(string name, string filePath, PackageViewModel packageViewModel)
        {
            Debug.Assert(name != null);
            Debug.Assert(filePath != null);
            Debug.Assert(packageViewModel != null);

            _filePath = filePath;
            _name = name;
            _packageViewModel = packageViewModel;
        }

        public string OriginalPath
        {
            get { return _filePath; }
        }

        public string Name
        {
            get { return _name; }
        }

        public string Path
        {
            get { return _name; }
        }

        public Stream GetStream()
        {
            return File.OpenRead(_filePath);
        }

        public bool Save(string editedFilePath)
        {
            return _packageViewModel.SaveMetadataAfterEditSource(editedFilePath);
        }

        public string EffectivePath
        {
            get { return _name; }
        }

        public FrameworkName TargetFramework
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