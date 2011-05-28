using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Windows.Input;
using NuGet;
using NuGetPackageExplorer.Types;

namespace PackageExplorerViewModel {
    public class PackageFile : PackagePart, IPackageFile {
        private readonly IPackageFile _file;
        private FileSystemWatcher _watcher;

        public PackageFile(IPackageFile file, string name, PackageViewModel viewModel)
            : this(file, name, null, viewModel) {
        }

        public PackageFile(IPackageFile file, string name, PackageFolder parent)
            : this(file, name, parent, parent.PackageViewModel) {
        }

        private PackageFile(IPackageFile file, string name, PackageFolder parent, PackageViewModel viewModel)
            : base(name, parent, viewModel) {
            if (file == null) {
                throw new ArgumentNullException("file");
            }

            _file = file;

            var physicalFile = file as PhysicalPackageFile;
            if (physicalFile != null) {
                WatchPhysicalFile(physicalFile);
            }
        }

        private void WatchPhysicalFile(PhysicalPackageFile physicalFile) {
            string folderPath = System.IO.Path.GetDirectoryName(physicalFile.SourcePath);
            string fileName = System.IO.Path.GetFileName(physicalFile.SourcePath);

            _watcher = new FileSystemWatcher(folderPath, fileName) {
                IncludeSubdirectories = false,
                EnableRaisingEvents = true
            };

            _watcher.Changed += new FileSystemEventHandler(OnFileChanged);
            _watcher.Deleted += new FileSystemEventHandler(OnFileDeleted);
            _watcher.Renamed += new RenamedEventHandler(OnFileDeleted);
        }

        private void OnFileChanged(object sender, FileSystemEventArgs e) {
            PackageViewModel.UIServices.BeginInvoke(PackageViewModel.NotifyChanges);
        }

        /// <summary>
        /// this is invoked on a background thread.
        /// </summary>
        private void OnFileDeleted(object sender, FileSystemEventArgs e) {
            PackageViewModel.UIServices.BeginInvoke(ShowMessageAndDeleteFile);
        }

        private void ShowMessageAndDeleteFile() {
            PackageViewModel.UIServices.Show(
                String.Format(CultureInfo.CurrentCulture, Resources.PhysicalFileMissing, Path),
                MessageLevel.Warning);
            Delete(false);
        }

        public override IEnumerable<IPackageFile> GetFiles() {
            yield return this;
        }

        public Stream GetStream() {
            return _file.GetStream();
        }

        public ICommand ViewCommand {
            get { return PackageViewModel.ViewContentCommand; }
        }

        public ICommand SaveCommand {
            get { return PackageViewModel.SaveContentCommand; }
        }

        public ICommand OpenCommand {
            get { return PackageViewModel.OpenContentFileCommand; }
        }

        public ICommand OpenWithCommand {
            get { return PackageViewModel.OpenWithContentFileCommand; }
        }

        public override void Export(string rootPath) {
            string fullPath = System.IO.Path.Combine(rootPath, Path);
            if (File.Exists(fullPath)) {
                bool confirmed = PackageViewModel.UIServices.Confirm(
                    Resources.ConfirmToReplaceFile_Title, 
                    String.Format(CultureInfo.CurrentCulture, Resources.ConfirmToReplaceFile, fullPath));
                if (!confirmed) {
                    return;
                }
            }

            using (var stream = File.Create(fullPath)) {
                GetStream().CopyTo(stream);
            }
        }

        protected override void Dispose(bool disposing) {
            if (_watcher != null) {
                _watcher.Deleted -= new FileSystemEventHandler(OnFileDeleted);
                _watcher.Renamed -= new RenamedEventHandler(OnFileDeleted);
                _watcher.Dispose();
                _watcher = null;
            }
            base.Dispose(disposing);
        }
    }
}
