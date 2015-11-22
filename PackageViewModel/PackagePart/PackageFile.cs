using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Runtime.Versioning;
using System.Windows.Input;
using NuGet;
using NuGetPackageExplorer.Types;

namespace PackageExplorerViewModel
{
    public class PackageFile : PackagePart, IPackageFile, IEditablePackageFile
    {
        private readonly IPackageFile _file;
        private FileSystemWatcher _watcher;

        public PackageFile(IPackageFile file, string name, PackageFolder parent)
            : this(file, name, parent, parent.PackageViewModel)
        {
        }

        private PackageFile(IPackageFile file, string name, PackageFolder parent, PackageViewModel viewModel)
            : base(name, parent, viewModel)
        {
            if (file == null)
            {
                throw new ArgumentNullException("file");
            }

            _file = file;

            var physicalFile = file as PhysicalPackageFile;
            if (physicalFile != null)
            {
                WatchPhysicalFile(physicalFile);
            }
            ReplaceCommand = new RelayCommand(Replace, () => !viewModel.IsInEditFileMode);
        }

        #region IPackageFile members

        /// <summary>
        /// Returns the path on disk if this file is a PhysicalPackageFile. Otherwise, returns null;
        /// </summary>
        public string OriginalPath
        {
            get { return _file.OriginalPath; }
        }

        public string EffectivePath
        {
            get { return _file.EffectivePath; }
        }

        public FrameworkName TargetFramework
        {
            get { return _file.TargetFramework; }
        }

        public IEnumerable<FrameworkName> SupportedFrameworks
        {
            get { return _file.SupportedFrameworks; }
        }

        public Stream GetStream()
        {
            return _file.GetStream();
        }

        #endregion

        public ICommand ViewCommand
        {
            get { return PackageViewModel.ViewContentCommand; }
        }

        public ICommand SaveCommand
        {
            get { return PackageViewModel.SaveContentCommand; }
        }

        public ICommand OpenCommand
        {
            get { return PackageViewModel.OpenContentFileCommand; }
        }

        public ICommand OpenWithCommand
        {
            get { return PackageViewModel.OpenWithContentFileCommand; }
        }

        public ICommand EditCommand
        {
            get { return PackageViewModel.EditFileCommand; }
        }

        public RelayCommand ReplaceCommand
        {
            get;
            private set;
        }

        private void WatchPhysicalFile(PhysicalPackageFile physicalFile)
        {
            string folderPath = System.IO.Path.GetDirectoryName(physicalFile.OriginalPath);
            string fileName = System.IO.Path.GetFileName(physicalFile.OriginalPath);

            _watcher = new FileSystemWatcher(folderPath, fileName)
                       {
                           IncludeSubdirectories = false,
                           EnableRaisingEvents = true
                       };

            _watcher.Changed += OnFileChanged;
            _watcher.Deleted += OnFileDeleted;
            _watcher.Renamed += OnFileDeleted;
        }

        private async void OnFileChanged(object sender, FileSystemEventArgs e)
        {
            await PackageViewModel.UIServices.BeginInvoke(PackageViewModel.NotifyChanges);
        }

        /// <summary>
        /// this is invoked on a background thread.
        /// </summary>
        private async void OnFileDeleted(object sender, FileSystemEventArgs e)
        {
            await PackageViewModel.UIServices.BeginInvoke(ShowMessageAndDeleteFile);
        }

        private void ShowMessageAndDeleteFile()
        {
            PackageViewModel.UIServices.Show(
                String.Format(CultureInfo.CurrentCulture, Resources.PhysicalFileMissing, Path),
                MessageLevel.Warning);
            Delete(false);
        }

        public override IEnumerable<IPackageFile> GetFiles()
        {
            yield return this;
        }

        public void Replace()
        {
            if (Parent != null)
            {
                Parent.ReplaceFile(this);
            }
        }

        public void ReplaceWith(string filePath)
        {
            if (Parent != null)
            {
                Parent.ReplaceFile(this, filePath);
            }
        }

        public override void Export(string rootPath)
        {
            string fullPath = System.IO.Path.Combine(rootPath, Path);
            if (File.Exists(fullPath))
            {
                bool confirmed = PackageViewModel.UIServices.Confirm(
                    Resources.ConfirmToReplaceFile_Title,
                    String.Format(CultureInfo.CurrentCulture, Resources.ConfirmToReplaceFile, fullPath));
                if (!confirmed)
                {
                    return;
                }
            }

            using (FileStream stream = File.Create(fullPath))
            {
                GetStream().CopyTo(stream);
            }
        }

        public bool Save(string editedFilePath)
        {
            if (!String.Equals(OriginalPath, editedFilePath, StringComparison.OrdinalIgnoreCase))
            {
                ReplaceWith(editedFilePath);
            }
            else if (PackageViewModel.IsShowingFileContent(this))
            {
                // force a refresh to show new content
                PackageViewModel.ShowFileContent(this);
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