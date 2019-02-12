using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Windows.Input;
using NuGet.Packaging;
using NuGetPackageExplorer.Types;
using NuGetPe;

namespace PackageExplorerViewModel
{
    public class PackageFolder : PackagePart
    {
        private ICommand? _addContentFolderCommand;
        private bool _isExpanded;

        public PackageFolder(string name, PackageFolder parent)
            : base(name, parent, parent.PackageViewModel)
        {
            Children = new SortedCollection<PackagePart>();
        }

        public PackageFolder(string name, PackageViewModel viewModel)
            : base(name, null, viewModel)
        {
            Children = new SortedCollection<PackagePart>();
        }

        public ICollection<PackagePart> Children { get; private set; }

        public ICommand AddContentFileCommand
        {
            get { return PackageViewModel.AddContentFileCommand; }
        }

        public ICommand AddNewFolderCommand
        {
            get { return PackageViewModel.AddNewFolderCommand; }
        }

        public ICommand AddNewFileCommand
        {
            get { return PackageViewModel.AddNewFileCommand; }
        }

        public ICommand AddScriptCommand
        {
            get { return PackageViewModel.AddScriptCommand; }
        }

        public ICommand AddBuildFileCommand
        {
            get { return PackageViewModel.AddBuildFileCommand; }
        }

        public ICommand AddContentFolderCommand
        {
            get
            {
                if (_addContentFolderCommand == null)
                {
                    _addContentFolderCommand = new RelayCommand<string>(AddContentFolderExecute,
                                                                        AddContentFolderCanExecute);
                }

                return _addContentFolderCommand;
            }
        }

        public bool IsExpanded
        {
            get { return _isExpanded; }
            set
            {
                if (_isExpanded != value)
                {
                    _isExpanded = value;
                    OnPropertyChanged("IsExpanded");
                }
            }
        }

        public PackagePart this[string name]
        {
            get { return Children.SingleOrDefault(p => p.Name.Equals(name, StringComparison.OrdinalIgnoreCase)); }
        }

        internal override void UpdatePath()
        {
            base.UpdatePath();

            foreach (var child in Children)
            {
                child.UpdatePath();
            }
        }

        public override IEnumerable<IPackageFile> GetFiles()
        {
            if (Children.Count == 0)
            {
                if (Parent != null)
                {
                    // only treat this folder as an empty folder if it's NOT the root folder.
                    return new[] { new EmptyFolderFile(Path) };
                }
                else
                {
                    return new IPackageFile[0];
                }
            }
            else
            {
                return Children.SelectMany(p => p.GetFiles());
            }
        }

        public override IEnumerable<PackagePart> GetPackageParts()
        {
            return new PackagePart[] { this }.Concat(Children.SelectMany(p => p.GetPackageParts()));
        }

        public void RemoveChild(PackagePart child)
        {
            if (child == null)
            {
                throw new ArgumentNullException("child");
            }

            var removed = Children.Remove(child);
            if (removed)
            {
                child.Dispose();
                PackageViewModel.NotifyChanges();
            }
        }

        private void Attach(PackagePart child)
        {
            child.Parent = this;
            Children.Add(child);
        }

        /// <summary>
        /// Detach() is different from Remove() in that it doesn't dispose the child.
        /// </summary>
        /// <param name="child"></param>
        private void Detach(PackagePart child)
        {
            Children.Remove(child);
            child.Parent = null;
        }

        private bool AddContentFolderCanExecute(string folderName)
        {
            if (folderName == null)
            {
                return false;
            }

            if (PackageViewModel.IsSigned || PackageViewModel.IsInEditFileMode)
            {
                return false;
            }

            return !ContainsFolder(folderName) && !ContainsFile(folderName);
        }

        private void AddContentFolderExecute(string folderName)
        {
            if (folderName == "portable")
            {
                if (!PackageViewModel.UIServices.TrySelectPortableFramework(out folderName))
                {
                    return;
                }
            }

            AddFolder(folderName);
        }

        public bool ContainsFolder(string folderName)
        {
            return Children.Any(p => p is PackageFolder && p.Name.Equals(folderName, StringComparison.OrdinalIgnoreCase));
        }

        public bool ContainsFile(string fileName)
        {
            return Children.Any(p => p is PackageFile && p.Name.Equals(fileName, StringComparison.OrdinalIgnoreCase));
        }

        public bool Contains(PackagePart child)
        {
            // we can't call Children.Contains(child) here because that will only check by file name, not the actual instance
            return Children.Any(p => p == child);
        }

        public PackageFolder? AddFolder(string folderName)
        {
            if (!AddContentFolderCanExecute(folderName))
            {
                PackageViewModel.UIServices.Show(
                    string.Format(CultureInfo.CurrentCulture, Resources.RenameCausesNameCollison, folderName),
                    MessageLevel.Error);
                return null;
            }

            var newFolder = new PackageFolder(folderName, this);
            AddFolderCore(newFolder);
            return newFolder;
        }

        public void AddFolder(PackageFolder childFolder, bool makeCopy = false)
        {
            if (!AddContentFolderCanExecute(childFolder.Name))
            {
                PackageViewModel.UIServices.Show(
                    string.Format(CultureInfo.CurrentCulture, Resources.RenameCausesNameCollison, childFolder.Name),
                    MessageLevel.Error);
                return;
            }

            if (IsDescendantOf(childFolder))
            {
                return;
            }

            PackageFolder newFolder;

            if (makeCopy)
            {
                newFolder = new PackageFolder(childFolder.Name, this);

                foreach (var child in childFolder.Children)
                {
                    if (child is PackageFile packageFile)
                    {
                        newFolder.AddFile(packageFile, true);
                    }
                    else if (child is PackageFolder packageFolder)
                    {
                        newFolder.AddFolder(packageFolder, true);
                    }
                }
            }
            else
            {
                if (childFolder.Parent != null)
                {
                    childFolder.Parent.Detach(childFolder);
                }

                newFolder = childFolder;
            }

            AddFolderCore(newFolder);
        }

        private void AddFolderCore(PackageFolder childFolder)
        {
            Attach(childFolder);
            childFolder.IsSelected = true;
            IsExpanded = true;
            PackageViewModel.NotifyChanges();
        }

        public PackageFile? AddFile(string filePath)
        {
            if (!File.Exists(filePath))
            {
                throw new ArgumentException("File does not exist.", "filePath");
            }

            var newFileName = System.IO.Path.GetFileName(filePath);
            if (ContainsFolder(newFileName))
            {
                PackageViewModel.UIServices.Show(Resources.FileNameConflictWithExistingDirectory, MessageLevel.Error);
                return null;
            }

            var showingRemovedFile = false;
            if (ContainsFile(newFileName))
            {
                var confirmed = PackageViewModel.UIServices.Confirm(
                    Resources.ConfirmToReplaceExsitingFile_Title,
                    string.Format(CultureInfo.CurrentCulture, Resources.ConfirmToReplaceExsitingFile, newFileName),
                    isWarning: true);

                if (confirmed)
                {
                    var part = this[newFileName] as PackageFile;
                    showingRemovedFile = PackageViewModel.IsShowingFileContent(part);

                    // remove the existing file before adding the new one
                    RemoveChildByName(newFileName);
                }
                else
                {
                    return null;
                }
            }

            var newTargetPath = Path + "\\" + newFileName;
            var physicalFile = new DiskPackageFile(newTargetPath, filePath);
            var newFile = new PackageFile(physicalFile, newFileName, this);

            Children.Add(newFile);
            newFile.IsSelected = true;
            IsExpanded = true;
            PackageViewModel.NotifyChanges();

            if (showingRemovedFile)
            {
                PackageViewModel.ShowFileContent(newFile);
            }

            return newFile;
        }

        public void AddFile(PackageFile file, bool makeCopy = false)
        {
            if (file == null)
            {
                throw new ArgumentNullException("file");
            }

            if (Contains(file))
            {
                return;
            }

            PackagePart newFile;

            if (makeCopy)
            {
                string fileCopyPath;
                using (var originalFileStream = file.GetStream())
                {
                    fileCopyPath = FileHelper.CreateTempFile(file.Name, originalFileStream);
                }

                var newTargetPath = Path + "\\" + file.Name;
                var physicalFile = new DiskPackageFile(newTargetPath, fileCopyPath);

                newFile = new PackageFile(physicalFile, file.Name, this);
            }
            else
            {
                // detach from current parent
                if (file.Parent != null)
                {
                    file.Parent.Detach(file);
                }

                newFile = file;
            }

            Attach(newFile);
            newFile.IsSelected = true;
            IsExpanded = true;
            PackageViewModel.NotifyChanges();
        }

        internal void ReplaceFile(PackageFile oldFile)
        {
            var result = PackageViewModel.UIServices.OpenFileDialog("Select New File", "All files (*.*)|*.*",
                                                                     out var selectedFileName);
            if (result)
            {
                ReplaceFile(oldFile, selectedFileName);
            }
        }

        internal void ReplaceFile(PackageFile oldFile, string newFilePath)
        {
            var showingFile = PackageViewModel.IsShowingFileContent(oldFile);

            // temporarily remove the old file in order to add a new file
            Children.Remove(oldFile);

            var newFile = AddFile(newFilePath);
            if (newFile != null)
            {
                // new file added successfully, officially delete the old file by disposing it
                oldFile.Dispose();

                if (showingFile)
                {
                    PackageViewModel.ShowFileContent(newFile);
                }
            }
            else
            {
                // otherwise, if the adding failed, restore the old file
                Children.Add(oldFile);
            }
        }

        public void AddPhysicalFolder(string folderPath)
        {
            var dirInfo = new DirectoryInfo(folderPath);
            if (!dirInfo.Exists)
            {
                return;
            }

            var folderName = dirInfo.Name;
            if (!AddContentFolderCanExecute(folderName))
            {
                PackageViewModel.UIServices.Show(
                    string.Format(CultureInfo.CurrentCulture, Resources.RenameCausesNameCollison, folderName),
                    MessageLevel.Error);
                return;
            }

            AddPhysicalFolderCore(dirInfo);
        }

        private void AddPhysicalFolderCore(DirectoryInfo dirInfo)
        {
            var childPackgeFolder = AddFolder(dirInfo.Name);
            foreach (var file in dirInfo.GetFiles("*.*", SearchOption.TopDirectoryOnly))
            {
                childPackgeFolder?.AddFile(file.FullName);
            }
            foreach (var subFolder in dirInfo.GetDirectories("*.*", SearchOption.TopDirectoryOnly))
            {
                childPackgeFolder?.AddPhysicalFolderCore(subFolder);
            }
        }

        private void RemoveChildByName(string name)
        {
            var count = Children.RemoveAll(p => p.Name.Equals(name, StringComparison.OrdinalIgnoreCase));
            Debug.Assert(count <= 1);
            if (count == 1)
            {
                PackageViewModel.NotifyChanges();
            }
        }

        public override void Export(string rootPath)
        {
            var fullPath = System.IO.Path.Combine(rootPath, Path);
            if (!Directory.Exists(fullPath))
            {
                Directory.CreateDirectory(fullPath);
            }

            foreach (var part in Children)
            {
                part.Export(rootPath);
            }
        }

        protected override void Dispose(bool disposing)
        {
            foreach (var part in Children)
            {
                part.Dispose();
            }
            base.Dispose(disposing);
        }
    }
}
