using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Windows.Input;
using NuGet;
using System.Globalization;
using NuGetPackageExplorer.Types;

namespace PackageExplorerViewModel {
    public class PackageFolder : PackagePart {

        public ICollection<PackagePart> Children { get; private set; }

        public PackageFolder(string name, PackageFolder parent)
            : base(name, parent, parent.PackageViewModel) {
            this.Children = new SortedCollection<PackagePart>();
        }

        public PackageFolder(string name, PackageViewModel viewModel)
            : base(name, null, viewModel) {
            this.Children = new SortedCollection<PackagePart>();
        }

        internal override void UpdatePath() {
            base.UpdatePath();

            if (Children != null) {
                foreach (var child in Children) {
                    child.UpdatePath();
                }
            }
        }

        public override IEnumerable<IPackageFile> GetFiles() {
            return Children.SelectMany(p => p.GetFiles());
        }

        public void RemoveChild(PackagePart child) {
            if (child == null) {
                throw new ArgumentNullException("child");
            }

            bool removed = Children.Remove(child);
            if (removed) {
                child.Dispose();
                PackageViewModel.NotifyChanges();
            }
        }

        private void Attach(PackagePart child) {
            Children.Add(child);
            child.Parent = this;
        }

        /// <summary>
        /// Detach() is different from Remove() in that it doesn't dispose the child.
        /// </summary>
        /// <param name="child"></param>
        private void Detach(PackagePart child) {
            Children.Remove(child);
            child.Parent = null;
        }

        public ICommand AddContentFileCommand {
            get {
                return PackageViewModel.AddContentFileCommand;
            }
        }

        public ICommand AddNewFolderCommand {
            get {
                return PackageViewModel.AddNewFolderCommand;
            }
        }

        private ICommand _addContentFolderCommand = null;
        public ICommand AddContentFolderCommand {
            get {
                if (_addContentFolderCommand == null) {
                    _addContentFolderCommand = new RelayCommand<string>(AddContentFolderExecute, AddContentFolderCanExecute);
                }

                return _addContentFolderCommand;
            }
        }

        private bool AddContentFolderCanExecute(string folderName) {
            if (folderName == null) {
                return false;
            }

            return !ContainsFolder(folderName) && !ContainsFile(folderName);
        }

        private void AddContentFolderExecute(string folderName) {
            AddFolder(folderName);
        }

        private bool _isExpanded;

        public bool IsExpanded {
            get {
                return _isExpanded;
            }
            set {
                if (_isExpanded != value) {
                    _isExpanded = value;
                    OnPropertyChanged("IsExpanded");
                }
            }
        }

        public PackagePart this[string name] {
            get {
                return Children.SingleOrDefault(p => p.Name.Equals(name, StringComparison.OrdinalIgnoreCase));
            }
        }

        public bool ContainsFolder(string folderName) {
            if (Children == null) {
                return false;
            }

            return Children.Any(p => p is PackageFolder && p.Name.Equals(folderName, StringComparison.OrdinalIgnoreCase));
        }

        public bool ContainsFile(string fileName) {
            if (Children == null) {
                return false;
            }

            return Children.Any(p => p is PackageFile && p.Name.Equals(fileName, StringComparison.OrdinalIgnoreCase));
        }

        public bool Contains(PackagePart child) {
            // we can't call Children.Contains(child) here because that will only check by file name, not the actual instance
            return Children != null && Children.Any(p => p == child);
        }

        public PackageFolder AddFolder(string folderName) {
            if (ContainsFolder(folderName) || ContainsFile(folderName)) {
                PackageViewModel.UIServices.Show(Resources.RenameCausesNameCollison, MessageLevel.Error);
                return null;
            }
            var newFolder = new PackageFolder(folderName, this);
            Children.Add(newFolder);
            newFolder.IsSelected = true;
            this.IsExpanded = true;
            PackageViewModel.NotifyChanges();
            return newFolder;
        }

        public PackageFile AddFile(string filePath) {
            if (!File.Exists(filePath)) {
                throw new ArgumentException("File does not exist.", "filePath");
            }

            string newFileName = System.IO.Path.GetFileName(filePath);
            if (ContainsFolder(newFileName)) {
                PackageViewModel.UIServices.Show(Resources.FileNameConflictWithExistingDirectory, MessageLevel.Error);
                return null;
            }

            bool showingRemovedFile = false;
            if (ContainsFile(newFileName)) {
                bool confirmed = PackageViewModel.UIServices.Confirm(
                    Resources.ConfirmToReplaceExsitingFile_Title, 
                    String.Format(CultureInfo.CurrentCulture, Resources.ConfirmToReplaceExsitingFile, newFileName),
                    isWarning: true);

                if (confirmed) {
                    PackageFile part = this[newFileName] as PackageFile;
                    showingRemovedFile = PackageViewModel.IsShowingFileContent(part);

                    // remove the existing file before adding the new one
                    RemoveChildByName(newFileName);
                }
                else {
                    return null;
                }
            }

            var physicalFile = new PhysicalPackageFile {
                SourcePath = filePath
            };
            var newFile = new PackageFile(physicalFile, newFileName, this);
            physicalFile.TargetPath = newFile.Path;
            Children.Add(newFile);
            newFile.IsSelected = true;
            this.IsExpanded = true;
            PackageViewModel.NotifyChanges();

            if (showingRemovedFile) {
                PackageViewModel.ShowFileContent(newFile);
            }

            return newFile;
        }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Design", "CA1011:ConsiderPassingBaseTypesAsParameters")]
        public void AddFile(PackageFile file) {
            if (file == null) {
                throw new ArgumentNullException("file");
            }

            if (Contains(file)) {
                return;
            }

            // detach from current parent
            if (file.Parent != null) {
                file.Parent.Detach(file);
            }

            Attach(file);
            file.IsSelected = true;
            this.IsExpanded = true;
            PackageViewModel.NotifyChanges();
        }

        private void RemoveChildByName(string name) {
            int count = Children.RemoveAll(p => p.Name.Equals(name, StringComparison.OrdinalIgnoreCase));
            Debug.Assert(count <= 1);
            if (count == 1) {
                PackageViewModel.NotifyChanges();
            }
        }

        public override void Export(string rootPath) {
            string fullPath = System.IO.Path.Combine(rootPath, Path);
            if (!Directory.Exists(fullPath)) {
                Directory.CreateDirectory(fullPath);
            }

            foreach (var part in Children) {
                part.Export(rootPath);
            }
        }

        protected override void Dispose(bool disposing) {
            foreach (var part in Children) {
                part.Dispose();
            }
            base.Dispose(disposing);
        }
    }
}
