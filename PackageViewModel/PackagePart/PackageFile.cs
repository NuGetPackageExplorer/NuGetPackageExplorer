using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Runtime.Versioning;

using System.Windows.Input;

using NuGet.Frameworks;
using NuGet.Packaging;
using NuGetPackageExplorer.Types;
using NuGetPe;
using NuGetPe.AssemblyMetadata;

namespace PackageExplorerViewModel
{
    [DebuggerDisplay("{Path}")]
    public class PackageFile : PackagePart, IFile, IEditablePackageFile, IPackageContent
    {
        private readonly IPackageFile _file;
        private FileSystemWatcher? _watcher;

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Design", "CA1062:Validate arguments of public methods", Justification = "<Pending>")]
        public PackageFile(IPackageFile file, string name, PackageFolder parent)
            : this(file, name, parent, parent.PackageViewModel)
        {
        }

        private PackageFile(IPackageFile file, string name, PackageFolder parent, PackageViewModel? viewModel)
            : base(name, parent, viewModel)
        {
            _file = file ?? throw new ArgumentNullException(nameof(file));

            if (file is DiskPackageFile physicalFile)
            {
                WatchPhysicalFile(physicalFile);
            }
            ReplaceCommand = new RelayCommand(Replace, () => !(viewModel?.IsSigned == true) && !(viewModel?.IsInEditFileMode == true));
        }

        /// <summary>
        /// Gets files in the same directory that end with PDB, XML, or PRI
        /// </summary>
        /// <returns></returns>
        public IEnumerable<IFile> GetAssociatedFiles()
        {
            var filename = System.IO.Path.GetFileNameWithoutExtension(Name);

            static bool HasSameName(IPart packagePart, string name) =>
                System.IO.Path.GetFileNameWithoutExtension(packagePart.Name).Equals(name, StringComparison.OrdinalIgnoreCase);

            return Parent!.GetFiles().Where(f => f.Path != Path && HasSameName(f, filename));
        }

        /// <summary>
        /// Gets files in the same directory that end with PDB, XML, or PRI
        /// </summary>
        /// <returns></returns>
        public IEnumerable<PackageFile> GetAssociatedPackageFiles()
        {
            const string niPdbExt = ".ni.pdb";

            string? filename = null;

            if (Name.EndsWith(niPdbExt, StringComparison.OrdinalIgnoreCase))
            {
                filename = Name.AsSpan()[..(Name.Length - niPdbExt.Length)].ToString();
            }

            filename ??= System.IO.Path.GetFileNameWithoutExtension(Name);

            static bool HasSameName(IPart packagePart, string name)
            {
                if (System.IO.Path.GetFileNameWithoutExtension(packagePart.Name).Equals(name, StringComparison.OrdinalIgnoreCase))
                    return true;

                if (packagePart.Name.EndsWith(niPdbExt, StringComparison.OrdinalIgnoreCase))
                {
                    var nameNoExt = packagePart.Name.AsSpan()[..(packagePart.Name.Length - niPdbExt.Length)];
                    return nameNoExt.CompareTo(name, StringComparison.OrdinalIgnoreCase) == 0;
                }
                return false;
            }

            return _parent!.GetFiles().OfType<PackageFile>().Where(f => f.Path != Path && HasSameName(f, filename));
        }

        /// <summary>
        /// Stores any debug data gathered for this PE file. Not set or null if not available
        /// </summary>
        public AssemblyDebugData? DebugData { get; set; }

        /// <summary>
        /// Returns the path on disk if this file is a PhysicalPackageFile. Otherwise, returns null;
        /// </summary>
        public string? OriginalPath
        {
            get { return _file.OriginalPath(); }
        }

        public string EffectivePath
        {
            get { return _file.EffectivePath; }
        }

        public FrameworkName TargetFramework
        {
#pragma warning disable CS0618 // Type or member is obsolete
            get { return _file.TargetFramework; }
#pragma warning restore CS0618 // Type or member is obsolete
        }
       // public NuGetFramework NuGetFramework => _file.NuGetFramework;

        public Stream GetStream()
        {
            return _file.GetStream();
        }


        public ICommand? ViewCommand
        {
            get { return PackageViewModel?.ViewContentCommand; }
        }

        public ICommand? SaveCommand
        {
            get { return PackageViewModel?.SaveContentCommand; }
        }

        public ICommand? OpenCommand
        {
            get { return PackageViewModel?.OpenContentFileCommand; }
        }

        public ICommand? OpenWithCommand
        {
            get { return PackageViewModel?.OpenWithContentFileCommand; }
        }

        public ICommand? EditCommand
        {
            get { return PackageViewModel?.EditFileCommand; }
        }

        public RelayCommand ReplaceCommand
        {
            get;
            private set;
        }

        public DateTimeOffset LastWriteTime => _file.LastWriteTime;

        public NuGetFramework NuGetFramework => _file.NuGetFramework;

        private void WatchPhysicalFile(DiskPackageFile physicalFile)
        {
            var folderPath = System.IO.Path.GetDirectoryName(physicalFile.OriginalPath);
            var fileName = System.IO.Path.GetFileName(physicalFile.OriginalPath);

            if (folderPath != null && fileName != null)
            {
                _watcher = new FileSystemWatcher(folderPath, fileName)
                {
                    IncludeSubdirectories = false,
                    EnableRaisingEvents = true
                };

                _watcher.Changed += OnFileChanged;
                _watcher.Deleted += OnFileDeleted;
                _watcher.Renamed += OnFileDeleted;

            }
        }

        private async void OnFileChanged(object sender, FileSystemEventArgs e)
        {
            if (PackageViewModel != null)
                await PackageViewModel.UIServices.BeginInvoke(PackageViewModel.NotifyChanges);
        }

        /// <summary>
        /// this is invoked on a background thread.
        /// </summary>
        private async void OnFileDeleted(object sender, FileSystemEventArgs e)
        {
            if(PackageViewModel != null)
                await PackageViewModel.UIServices.BeginInvoke(ShowMessageAndDeleteFile);
        }

        private void ShowMessageAndDeleteFile()
        {
            PackageViewModel?.UIServices.Show(
                string.Format(CultureInfo.CurrentCulture, Resources.PhysicalFileMissing, Path),
                MessageLevel.Warning);
            Delete(false);
        }

        public override IEnumerable<IFile> GetFiles()
        {
            yield return this;
        }

        public override IEnumerable<IPackageFile> GetPackageFiles()
        {
            yield return this;
        }

        public override IEnumerable<PackagePart> GetPackageParts()
        {
            yield return this;
        }

        public void Replace()
        {
            _parent?.ReplaceFile(this);
        }

        public void ReplaceWith(string filePath)
        {
            _parent?.ReplaceFile(this, filePath);
        }

        public override void Export(string rootPath)
        {
            var fullPath = System.IO.Path.Combine(rootPath, Path);
            if (File.Exists(fullPath))
            {
                var confirmed = PackageViewModel?.UIServices.Confirm(
                    Resources.ConfirmToReplaceFile_Title,
                    string.Format(CultureInfo.CurrentCulture, Resources.ConfirmToReplaceFile, fullPath)) ?? true;
                if (!confirmed)
                {
                    return;
                }
            }

            {
                using var stream = File.Create(fullPath);
                using var packageStream = GetStream();
                packageStream.CopyTo(stream);
            }
            File.SetLastWriteTime(fullPath, LastWriteTime.DateTime);
        }

        public bool Save(string editedFilePath)
        {
            if (!string.Equals(OriginalPath, editedFilePath, StringComparison.OrdinalIgnoreCase))
            {
                ReplaceWith(editedFilePath);
            }
            else if (PackageViewModel?.IsShowingFileContent(this) == true)
            {
                // force a refresh to show new content
                PackageViewModel?.ShowFileContent(this);
            }

            return true;
        }

        protected override void Dispose(bool disposing)
        {
            if (_watcher != null)
            {
                _watcher.Deleted -= OnFileDeleted;
                _watcher.Renamed -= OnFileDeleted;
                _watcher.Dispose();
                _watcher = null;
            }
            base.Dispose(disposing);
        }
    }
}
