using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net.NetworkInformation;
using System.Windows.Input;
using NuGetPe;
using NuGetPackageExplorer.Types;
using LazyPackageCommand = System.Lazy<NuGetPackageExplorer.Types.IPackageCommand, NuGetPackageExplorer.Types.IPackageCommandMetadata>;
using NuGet.Packaging;

namespace PackageExplorerViewModel
{
    public sealed class PackageViewModel : ViewModelBase, IDisposable
    {
        #region private fields

        private readonly IList<Lazy<IPackageContentViewer, IPackageContentViewerMetadata>> _contentViewerMetadata;
        private readonly IPackageEditorService _editorService;
        private readonly IMruManager _mruManager;
        private readonly IPackage _package;
        private readonly ObservableCollection<PackageIssue> _packageIssues = new ObservableCollection<PackageIssue>();
        private EditablePackageMetadata _packageMetadata;
        private readonly PackageFolder _packageRoot;
        private readonly IList<Lazy<IPackageRule>> _packageRules;
        private readonly ISettingsManager _settingsManager;
        private readonly IUIServices _uiServices;

        private ICommand _addContentFileCommand;
        private ICommand _addContentFolderCommand;
        private ICommand _addNewFileCommand;
        private ICommand _addNewFolderCommand;
        private ICommand _addScriptCommand;
        private ICommand _addBuildFileCommand;
        private ICommand _applyEditCommand;
        private ICommand _cancelEditCommand;
        private FileContentInfo _currentFileInfo;
        private RelayCommand<object> _deleteContentCommand;
        private ICommand _editCommand;
        private ICommand _editFileCommand;
        private ICommand _editMetadataSourceCommand;
        private ICommand _executePackageCommand;
        private RelayCommand _exportCommand;
        private FileEditorViewModel _fileEditorViewModel;
        private bool _hasEdit;
        private bool _isInEditMode;
        private RelayCommand<object> _openContentFileCommand;
        private ICommand _openWithContentFileCommand;
        private string _packageSource;
        private RelayCommand _publishCommand;
        private RelayCommand<object> _renameContentCommand;
        private SavePackageCommand _saveCommand;
        private ICommand _saveContentCommand;
        private object _selectedItem;
        private bool _showContentViewer;
        private bool _showPackageAnalysis;
        private ICommand _viewContentCommand;
        private ICommand _viewPackageAnalysisCommand;

        #endregion

        internal PackageViewModel(
            IPackage package,
            string source,
            IMruManager mruManager,
            IUIServices uiServices,
            IPackageEditorService editorService,
            ISettingsManager settingsManager,
            IList<Lazy<IPackageContentViewer, IPackageContentViewerMetadata>> contentViewerMetadata,
            IList<Lazy<IPackageRule>> packageRules)
        {
            if (package == null)
            {
                throw new ArgumentNullException("package");
            }
            if (mruManager == null)
            {
                throw new ArgumentNullException("mruManager");
            }
            if (uiServices == null)
            {
                throw new ArgumentNullException("uiServices");
            }
            if (editorService == null)
            {
                throw new ArgumentNullException("editorService");
            }
            if (settingsManager == null)
            {
                throw new ArgumentNullException("settingsManager");
            }

            _settingsManager = settingsManager;
            _editorService = editorService;
            _uiServices = uiServices;
            _mruManager = mruManager;
            _package = package;
            _contentViewerMetadata = contentViewerMetadata;
            _packageRules = packageRules;

            _packageMetadata = new EditablePackageMetadata(_package, _uiServices);

            PackageSource = source;

            _packageRoot = PathToTreeConverter.Convert(_package.GetFiles().ToList(), this);
        }



        internal IList<Lazy<IPackageContentViewer, IPackageContentViewerMetadata>> ContentViewerMetadata
        {
            get { return _contentViewerMetadata; }
        }

        internal IUIServices UIServices
        {
            get { return _uiServices; }
        }

        public bool IsInEditMetadataMode
        {
            get { return _isInEditMode; }
            private set
            {
                if (_isInEditMode != value)
                {
                    _isInEditMode = value;
                    OnPropertyChanged("IsInEditMetadataMode");
                }
            }
        }

        public bool IsInEditFileMode
        {
            get { return FileEditorViewModel != null; }
        }

        public bool IsReadOnly => _package.IsSigned; // Signed packages can not be altered. Remove the sig first
        

        public FileEditorViewModel FileEditorViewModel
        {
            get { return _fileEditorViewModel; }
            set
            {
                if (_fileEditorViewModel != value)
                {
                    _fileEditorViewModel = value;
                    OnPropertyChanged("FileEditorViewModel");
                    OnPropertyChanged("IsInEditFileMode");
                }
            }
        }

        public string WindowTitle
        {
            get { return Resources.Dialog_Title + " - " + _packageMetadata; }
        }

        public EditablePackageMetadata PackageMetadata
        {
            get { return _packageMetadata; }
            private set
            {
                if (_packageMetadata != value)
                {
                    _packageMetadata = value;
                    OnPropertyChanged("PackageMetadata");
                }
            }
        }

        public bool ShowContentViewer
        {
            get { return _showContentViewer; }
            set
            {
                if (_showContentViewer != value)
                {
                    _showContentViewer = value;
                    OnPropertyChanged("ShowContentViewer");
                }
            }
        }

        public bool ShowPackageAnalysis
        {
            get { return _showPackageAnalysis; }
            set
            {
                if (_showPackageAnalysis != value)
                {
                    _showPackageAnalysis = value;
                    OnPropertyChanged("ShowPackageAnalysis");
                }
            }
        }

        public FileContentInfo CurrentFileInfo
        {
            get { return _currentFileInfo; }
            set
            {
                if (_currentFileInfo != value)
                {
                    _currentFileInfo = value;
                    OnPropertyChanged("CurrentFileInfo");
                }
            }
        }

        public ICollection<PackagePart> PackageParts
        {
            get { return _packageRoot.Children; }
        }

        public object SelectedItem
        {
            get { return _selectedItem; }
            set
            {
                if (_selectedItem != value)
                {
                    _selectedItem = value;
                    OnPropertyChanged("SelectedItem");
                    ((ViewContentCommand) ViewContentCommand).RaiseCanExecuteChanged();
                    CommandManager.InvalidateRequerySuggested();
                }
            }
        }

        public string PackageSource
        {
            get { return _packageSource; }
            set
            {
                if (_packageSource != value)
                {
                    _packageSource = value;
                    OnPropertyChanged("PackageSource");
                }
            }
        }

        public bool HasEdit
        {
            get { return _hasEdit; }
            set
            {
                if (_hasEdit != value)
                {
                    _hasEdit = value;
                    OnPropertyChanged("HasEdit");
                }
            }
        }

        public ObservableCollection<PackageIssue> PackageIssues
        {
            get { return _packageIssues; }
        }

        public PackageFolder RootFolder
        {
            get { return _packageRoot; }
        }

        #region IDisposable Members

        public void Dispose()
        {
            RootFolder.Dispose();
            _package.Dispose();
        }

        #endregion

        #region AddContentFileCommand

        public ICommand AddContentFileCommand
        {
            get
            {
                if (_addContentFileCommand == null)
                {
                    _addContentFileCommand = new RelayCommand<object>(AddContentFileExecute, AddContentFileCanExecute);
                }

                return _addContentFileCommand;
            }
        }

        private bool AddContentFileCanExecute(object parameter)
        {
            if (IsReadOnly || IsInEditFileMode)
            {
                return false;
            }

            parameter = parameter ?? SelectedItem;
            return parameter == null || parameter is PackageFolder;
        }

        private void AddContentFileExecute(object parameter)
        {
            var folder = (parameter ?? SelectedItem) as PackageFolder;
            AddExistingFileToFolder(folder ?? RootFolder);
        }

        private void AddExistingFileToFolder(PackageFolder folder)
        {
            string[] selectedFiles;
            bool result = UIServices.OpenMultipleFilesDialog(
                "Select Files",
                "All files (*.*)|*.*",
                out selectedFiles);

            if (result)
            {
                foreach (string file in selectedFiles)
                {
                    folder.AddFile(file, isTempFile: false);
                }
            }
        }

        #endregion

        #region AddContentFolderCommand

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

        private bool AddContentFolderCanExecute(string folderName)
        {
            if (folderName == null)
            {
                return false;
            }

            if (IsReadOnly || IsInEditFileMode)
            {
                return false;
            }

            return !RootFolder.ContainsFolder(folderName);
        }

        private void AddContentFolderExecute(string folderName)
        {
            RootFolder.AddFolder(folderName);
        }

        #endregion

        #region AddNewFolderCommand

        public ICommand AddNewFolderCommand
        {
            get
            {
                if (_addNewFolderCommand == null)
                {
                    _addNewFolderCommand = new RelayCommand<object>(AddNewFolderExecute, AddNewFolderCanExecute);
                }

                return _addNewFolderCommand;
            }
        }

        private bool AddNewFolderCanExecute(object parameter)
        {
            if (IsReadOnly || IsInEditFileMode)
            {
                return false;
            }

            parameter = parameter ?? SelectedItem;
            return parameter == null || parameter is PackageFolder;
        }

        private void AddNewFolderExecute(object parameter)
        {
            parameter = parameter ?? SelectedItem ?? RootFolder;
            var folder = parameter as PackageFolder;
            string folderName = "NewFolder";
            bool result = UIServices.OpenRenameDialog(
                folderName,
                "Provide name for the new folder.",
                out folderName);
            if (result)
            {
                folder.AddFolder(folderName);
            }
        }

        #endregion

        #region SavePackageCommand

        public ICommand SaveCommand
        {
            get
            {
                if (_saveCommand == null)
                {
                    _saveCommand = new SavePackageCommand(this);
                }
                return _saveCommand;
            }
        }

        #endregion

        #region EditPackageCommand

        public ICommand EditCommand
        {
            get
            {
                if (_editCommand == null)
                {
                    _editCommand = new RelayCommand(EditPackageExecute, EditPackageCanExecute);
                }
                return _editCommand;
            }
        }

        private bool EditPackageCanExecute()
        {
            return !IsReadOnly && !IsInEditMetadataMode && !IsInEditFileMode;
        }

        private void EditPackageExecute()
        {
            _editorService.BeginEdit();
            BeginEdit();
        }

        #endregion

        #region ApplyEditCommand

        public ICommand ApplyEditCommand
        {
            get
            {
                if (_applyEditCommand == null)
                {
                    _applyEditCommand = new RelayCommand(() => ApplyEditExecute(), () => !IsReadOnly && !IsInEditFileMode);
                }

                return _applyEditCommand;
            }
        }

        internal bool ApplyEditExecute()
        {
            bool valid = _editorService.CommitEdit();
            if (valid)
            {
                CommitEdit();
            }

            return valid;
        }

        #endregion

        #region CancelEditCommand

        public ICommand CancelEditCommand
        {
            get
            {
                if (_cancelEditCommand == null)
                {
                    _cancelEditCommand = new RelayCommand(CancelEditExecute, () => !IsReadOnly && !IsInEditFileMode);
                }

                return _cancelEditCommand;
            }
        }

        private void CancelEditExecute()
        {
            _editorService.CancelEdit();
            CancelEdit();
        }

        #endregion

        #region DeleteContentCommand

        public RelayCommand<object> DeleteContentCommand
        {
            get
            {
                if (_deleteContentCommand == null)
                {
                    _deleteContentCommand = new RelayCommand<object>(DeleteContentExecute, DeleteContentCanExecute);
                }

                return _deleteContentCommand;
            }
        }

        private bool DeleteContentCanExecute(object parameter)
        {
            if (IsReadOnly || IsInEditFileMode)
            {
                return false;
            }

            return (parameter ?? SelectedItem) is PackagePart;
        }

        private void DeleteContentExecute(object parameter)
        {
            var file = (parameter ?? SelectedItem) as PackagePart;
            if (file != null)
            {
                file.Delete();
            }
        }

        #endregion

        #region RenameContentCommand

        public RelayCommand<object> RenameContentCommand
        {
            get
            {
                if (_renameContentCommand == null)
                {
                    _renameContentCommand = new RelayCommand<object>(RenameContentExecuted, RenameContentCanExecuted);
                }
                return _renameContentCommand;
            }
        }

        private bool RenameContentCanExecuted(object parameter)
        {
            if (IsReadOnly || IsInEditFileMode)
            {
                return false;
            }

            return (parameter ?? SelectedItem) is PackagePart;
        }

        private void RenameContentExecuted(object parameter)
        {
            var part = (parameter ?? SelectedItem) as PackagePart;
            if (part != null)
            {
                string newName;
                bool result = UIServices.OpenRenameDialog(
                    part.Name,
                    "Provide new name for '" + part.Name + "'.",
                    out newName);

                if (result)
                {
                    part.Rename(newName);
                }
            }
        }

        #endregion

        #region OpenContentFileCommand

        public RelayCommand<object> OpenContentFileCommand
        {
            get
            {
                if (_openContentFileCommand == null)
                {
                    _openContentFileCommand = new RelayCommand<object>(OpenContentFileExecute, OpenContentFileCanExecute);
                }
                return _openContentFileCommand;
            }
        }

        private bool OpenContentFileCanExecute(object parameter)
        {
            if (IsInEditFileMode)
            {
                return false;
            }

            parameter = parameter ?? SelectedItem;
            return parameter is PackageFile;
        }

        private void OpenContentFileExecute(object parameter)
        {
            parameter = parameter ?? SelectedItem;
            var file = parameter as PackageFile;
            if (file != null)
            {
                FileHelper.OpenFileInShell(file, UIServices);
            }
        }

        #endregion

        #region OpenWithContentFileCommand

        public ICommand OpenWithContentFileCommand
        {
            get
            {
                if (_openWithContentFileCommand == null)
                {
                    _openWithContentFileCommand = new RelayCommand<PackageFile>(FileHelper.OpenFileInShellWith,
                                                                                f => !IsInEditFileMode);
                }
                return _openWithContentFileCommand;
            }
        }

        #endregion

        #region SaveContentCommand

        public ICommand SaveContentCommand
        {
            get
            {
                if (_saveContentCommand == null)
                {
                    _saveContentCommand = new RelayCommand<PackageFile>(SaveContentExecute, SaveContentCanExecute);
                }
                return _saveContentCommand;
            }
        }

        private void SaveContentExecute(PackageFile file)
        {
            string selectedFileName;
            string title = "Save " + file.Name;
            const string filter = "All files (*.*)|*.*";
            int filterIndex;
            if (UIServices.OpenSaveFileDialog(title, file.Name, /* initial directory */ null, filter, /* overwritePrompt */ true, 
                                              out selectedFileName, out filterIndex))
            {
                using (FileStream fileStream = File.OpenWrite(selectedFileName))
                {
                    file.GetStream().CopyTo(fileStream);
                }
            }
        }

        private bool SaveContentCanExecute(PackageFile file)
        {
            return !IsReadOnly && !IsInEditFileMode;
        }

        #endregion

        #region ViewContentCommand

        public ICommand ViewContentCommand
        {
            get
            {
                if (_viewContentCommand == null)
                {
                    _viewContentCommand = new ViewContentCommand(this);
                }
                return _viewContentCommand;
            }
        }

        #endregion

        #region PublishCommand

        public RelayCommand PublishCommand
        {
            get
            {
                if (_publishCommand == null)
                {
                    _publishCommand = new RelayCommand(PublishExecute, PublishCanExecute);
                }
                return _publishCommand;
            }
        }

        private void PublishExecute()
        {
            if (!NetworkInterface.GetIsNetworkAvailable())
            {
                UIServices.Show(Resources.NoNetworkConnection, MessageLevel.Warning);
                return;
            }

            // validate the package to see if there is any error before actually creating the package.
            PackageIssue firstIssue = Validate().FirstOrDefault(p => p.Level == PackageIssueLevel.Error);
            if (firstIssue != null)
            {
                UIServices.Show(
                    Resources.PackageCreationFailed
                    + Environment.NewLine
                    + Environment.NewLine
                    + firstIssue.Description,
                    MessageLevel.Warning);
                return;
            }

            using (var mruSourceManager = new MruPackageSourceManager(
                new PublishSourceSettings(_settingsManager)))
            {
                var publishPackageViewModel = new PublishPackageViewModel(
                    mruSourceManager,
                    _settingsManager,
                    this);
                _uiServices.OpenPublishDialog(publishPackageViewModel);
            }
        }

        private bool PublishCanExecute()
        {
            return !IsInEditMetadataMode && !IsInEditFileMode;
        }

        #endregion

        #region ExportCommand

        private string _folderPath = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);

        public RelayCommand ExportCommand
        {
            get
            {
                if (_exportCommand == null)
                {
                    _exportCommand = new RelayCommand(ExportExecute, ExportCanExecute);
                }
                return _exportCommand;
            }
        }

        [SuppressMessage("Microsoft.Design", "CA1031:DoNotCatchGeneralExceptionTypes")]
        private void ExportExecute()
        {
            string rootPath;
            if (_uiServices.OpenFolderDialog("Choose a folder to export package to:", _folderPath, out rootPath))
            {
                try
                {
                    Export(rootPath);
                    UIServices.Show(Resources.ExportPackageSuccess, MessageLevel.Information);
                }
                catch (Exception ex)
                {
                    UIServices.Show(ex.Message, MessageLevel.Error);
                }

                _folderPath = rootPath;
            }
        }

        private bool ExportCanExecute()
        {
            return !IsInEditMetadataMode && !IsInEditFileMode;
        }

        #endregion

        #region ExecutePackageCommand

        public ICommand ExecutePackageCommand
        {
            get
            {
                if (_executePackageCommand == null)
                {
                    _executePackageCommand = new RelayCommand<LazyPackageCommand>(PackageCommandExecute,
                                                                                  p => !IsInEditFileMode);
                }
                return _executePackageCommand;
            }
        }

        [SuppressMessage("Microsoft.Design", "CA1031:DoNotCatchGeneralExceptionTypes"),
         SuppressMessage("Microsoft.Globalization", "CA1303:Do not pass literals as localized parameters",
             MessageId =
                 "NuGetPackageExplorer.Types.IUIServices.Show(System.String,NuGetPackageExplorer.Types.MessageLevel)")]
        private void PackageCommandExecute(LazyPackageCommand packageCommand)
        {
            IPackage package = PackageHelper.BuildPackage(PackageMetadata, GetFiles());
            try
            {
                packageCommand.Value.Execute(package, PackageSource);
            }
            catch (Exception ex)
            {
                UIServices.Show("The command failed with this error message:" +
                                Environment.NewLine +
                                Environment.NewLine +
                                ex.Message, MessageLevel.Error);
            }
        }

        #endregion

        #region ViewPackageAnalysisCommand

        public ICommand ViewPackageAnalysisCommand
        {
            get
            {
                if (_viewPackageAnalysisCommand == null)
                {
                    _viewPackageAnalysisCommand = new RelayCommand<string>(ViewPackageAnalysisExecute,
                                                                           CanExecutePackageAnalysis);
                }
                return _viewPackageAnalysisCommand;
            }
        }

        private void ViewPackageAnalysisExecute(string parameter)
        {
            if (parameter == "Hide")
            {
                ShowPackageAnalysis = false;
            }
            else if (_packageRules != null)
            {
                IEnumerable<PackageIssue> allIssues = Validate().OrderBy(p => p.Title, StringComparer.CurrentCulture);
                SetPackageIssues(allIssues);
                ShowPackageAnalysis = true;
            }
        }

        private bool CanExecutePackageAnalysis(string parameter)
        {
            if (IsInEditFileMode)
            {
                return false;
            }

            return parameter == "Hide" || !IsInEditMetadataMode;
        }

        #endregion

        #region EditFileCommand

        public ICommand EditFileCommand
        {
            get
            {
                if (_editFileCommand == null)
                {
                    _editFileCommand = new RelayCommand<PackagePart>(EditFileCommandExecute, CanEditFileCommandExecute);
                }

                return _editFileCommand;
            }
        }

        private void EditFileCommandExecute(PackagePart file)
        {
            // before editing file, try to commit metadata pending changes to avoid data loss
            if (IsInEditMetadataMode)
            {
                bool isMetadataValid = ApplyEditExecute();
                if (!isMetadataValid)
                {
                    UIServices.Show(Resources.EditFormHasInvalidInput, MessageLevel.Error);
                    return;
                }
            }

            FileEditorViewModel = new FileEditorViewModel(this, file as PackageFile);
        }

        private bool CanEditFileCommandExecute(PackagePart file)
        {
            return !IsReadOnly && 
                   (file is PackageFile) && 
                   !IsInEditFileMode &&
                   !FileHelper.IsBinaryFile(file.Path);
        }

        internal void CloseEditFileMode()
        {
            FileEditorViewModel = null;
        }

        #endregion

        #region EditMetadataSourceCommand

        public ICommand EditMetadataSourceCommand
        {
            get
            {
                if (_editMetadataSourceCommand == null)
                {
                    _editMetadataSourceCommand = new RelayCommand(EditMetadataSourceCommandExecute, CanEditMetadataSourceCommandExecute);
                }

                return _editMetadataSourceCommand;
            }
        }

        private void EditMetadataSourceCommandExecute()
        {
            FileEditorViewModel = new FileEditorViewModel(this, CreatePackageMetadataFile());
        }

        private bool CanEditMetadataSourceCommandExecute()
        {
            return !IsReadOnly && !IsInEditFileMode && !IsInEditMetadataMode;
        }

        private IEditablePackageFile CreatePackageMetadataFile()
        {
            string packageName = PackageMetadata + NuGetPe.Constants.ManifestExtension;
            string filePath = Path.GetTempFileName();
            
            ExportManifest(filePath, askForConfirmation: false, includeFilesSection: false);

            return new PackageMetadataFile(packageName, filePath, this);
        }

        #endregion

        #region AddNewFileCommand

        public ICommand AddNewFileCommand
        {
            get
            {
                if (_addNewFileCommand == null)
                {
                    _addNewFileCommand = new RelayCommand<object>(AddNewFileExecute, AddNewFileCanExecute);
                }

                return _addNewFileCommand;
            }
        }

        private bool AddNewFileCanExecute(object parameter)
        {
            if (IsReadOnly || IsInEditFileMode)
            {
                return false;
            }

            parameter = parameter ?? SelectedItem;
            return parameter == null || parameter is PackageFolder;
        }

        private void AddNewFileExecute(object parameter)
        {
            var folder = (parameter ?? SelectedItem) as PackageFolder;
            AddNewFileToFolder(folder ?? RootFolder);
        }

        private void AddNewFileToFolder(PackageFolder folder)
        {
            string newName;
            bool result = UIServices.OpenRenameDialog(
                "NewFile.txt",
                "Provide name for the new file.",
                out newName);
            if (result)
            {
                string sourcePath = FileHelper.CreateTempFile(newName);
                PackageFile file = folder.AddFile(sourcePath, isTempFile: true);
                // file can be null if it collides with other files in the same directory
                if (file != null)
                {
                    EditFileCommandExecute(file);
                }
            }
        }

        #endregion

        #region AddScriptCommand 

        public ICommand AddScriptCommand
        {
            get
            {
                if (_addScriptCommand == null)
                {
                    _addScriptCommand = new RelayCommand<string>(AddScriptCommandExecute, AddScriptCommandCanExecute);
                }

                return _addScriptCommand;
            }
        }

        private void AddScriptCommandExecute(string scriptName)
        {
            string content = scriptName.Equals("init.ps1", StringComparison.OrdinalIgnoreCase)
                                 ? Constants.ContentForInit
                                 : Constants.ContentForInstall;
            string sourcePath = FileHelper.CreateTempFile(scriptName, content);

            var selectedFolder = SelectedItem as PackageFolder;
            if (selectedFolder != null)
            {
                PackageFile file = selectedFolder.AddFile(sourcePath, isTempFile: true);
                // file can be null if it collides with other files in the same directory
                if (file != null)
                {
                    EditFileCommandExecute(file);
                }
            }
        }

        private bool AddScriptCommandCanExecute(string scriptName)
        {
            if (scriptName != "install.ps1" && scriptName != "init.ps1" && scriptName != "uninstall.ps1")
            {
                return false;
            }

            var selectedFolder = SelectedItem as PackageFolder;
            return selectedFolder != null && !selectedFolder.ContainsFile(scriptName);
        }

        #endregion

        #region AddBuildFileCommand

        public ICommand AddBuildFileCommand
        {
            get
            {
                if (_addBuildFileCommand == null)
                {
                    _addBuildFileCommand = new RelayCommand<string>(AddBuildFileCommandExecute, AddBuildFileCommandCanExecute);
                }

                return _addBuildFileCommand;
            }
        }

        private bool AddBuildFileCommandCanExecute(string extension)
        {
            var selectedFolder = SelectedItem as PackageFolder;

            if (selectedFolder != null)
            {
                string fileName = PackageMetadata.Id + extension;
                return !selectedFolder.ContainsFile(fileName);
            }

            return false;
        }

        private void AddBuildFileCommandExecute(string extension)
        {
            string fileName = PackageMetadata.Id + extension;
            string sourcePath = FileHelper.CreateTempFile(fileName, Constants.ContentForBuildFile);

            var selectedFolder = SelectedItem as PackageFolder;
            if (selectedFolder != null)
            {
                PackageFile file = selectedFolder.AddFile(sourcePath, isTempFile: true);
                // file can be null if it collides with other files in the same directory
                if (file != null)
                {
                    EditFileCommandExecute(file);
                }
            }
        }

        #endregion

        private void SetPackageIssues(IEnumerable<PackageIssue> issues)
        {
            _packageIssues.Clear();
            _packageIssues.AddRange(issues);
        }

        public void ShowFile(FileContentInfo fileInfo)
        {
            ShowContentViewer = true;
            CurrentFileInfo = fileInfo;
        }

        [SuppressMessage("Microsoft.Design", "CA1024:UsePropertiesWhereAppropriate")]
        internal IEnumerable<IPackageFile> GetFiles()
        {
            return _packageRoot.GetFiles();
        }

        [SuppressMessage("Microsoft.Design", "CA1024:UsePropertiesWhereAppropriate")]
        public string GetCurrentPackageTempFile()
        {
            string tempFile = Path.GetTempFileName();
            PackageHelper.SavePackage(PackageMetadata, GetFiles(), tempFile, useTempFile: false);
            if (File.Exists(tempFile))
            {
                return tempFile;
            }

            return null;
        }

        public void BeginEdit()
        {
            // raise the property change event here to force the edit form to rebind 
            // all controls, which will erase all error states, if any, left over from the previous edit
            OnPropertyChanged("PackageMetadata");
            IsInEditMetadataMode = true;
        }

        public void CancelEdit()
        {
            PackageMetadata.ResetErrors();
            IsInEditMetadataMode = false;
        }

        private void CommitEdit()
        {
            HasEdit = true;
            PackageMetadata.ResetErrors();
            IsInEditMetadataMode = false;
            OnPropertyChanged("WindowTitle");
        }

        internal void OnSaved(string fileName)
        {
            HasEdit = false;
            _mruManager.NotifyFileAdded(PackageMetadata, fileName, PackageType.LocalPackage);
        }

        internal void NotifyChanges()
        {
            HasEdit = true;
        }

        public IEnumerable<PackageIssue> Validate()
        {
            IPackage package = PackageHelper.BuildPackage(PackageMetadata, GetFiles());
            string packageFileName = Path.IsPathRooted(PackageSource) ? Path.GetFileName(PackageSource) : null;
            return package.Validate(_packageRules.Select(r => r.Value), packageFileName);
        }

        private void Export(string rootPath)
        {
            if (rootPath == null)
            {
                throw new ArgumentNullException("rootPath");
            }

            if (!Directory.Exists(rootPath))
            {
                throw new ArgumentException("Specified directory doesn't exist.");
            }

            // export files
            RootFolder.Export(rootPath);

            // export .nuspec file
            ExportManifest(Path.Combine(rootPath, PackageMetadata + ".nuspec"));
        }

        internal void ExportManifest(string fullpath, bool askForConfirmation = true, bool includeFilesSection = true)
        {
            if (File.Exists(fullpath) && askForConfirmation)
            {
                bool confirmed = UIServices.Confirm(
                    Resources.ConfirmToReplaceFile_Title,
                    String.Format(CultureInfo.CurrentCulture, Resources.ConfirmToReplaceFile, fullpath));
                if (!confirmed)
                {
                    return;
                }
            }

            string rootPath = Path.GetDirectoryName(fullpath);

            using (Stream fileStream = File.Create(fullpath))
            {
                Manifest manifest = Manifest.Create(PackageMetadata);
                if (includeFilesSection)
                {
                    string tempPath = Path.GetTempPath();
                    
                    manifest.Files.AddRange(RootFolder.GetFiles().Select(
                        f => new ManifestFile
                        {
                            Source = String.IsNullOrEmpty(f.OriginalPath()) || f.OriginalPath().StartsWith(tempPath, StringComparison.OrdinalIgnoreCase) ? f.Path : PathUtility.RelativePathTo(rootPath, f.OriginalPath()),
                            Target = f.Path
                        })
                    );
                }
                manifest.Save(fileStream);
            }
        }

        internal void NotifyContentRenamed(PackagePart packagePart)
        {
            if (CurrentFileInfo != null && CurrentFileInfo.File.IsDescendantOf(packagePart))
            {
                ShowFileContent(CurrentFileInfo.File);
            }

            NotifyChanges();
        }

        internal void NotifyContentDeleted(PackagePart packagePart)
        {
            // if the deleted file is being shown in the content pane, close the content pane
            if (CurrentFileInfo != null && CurrentFileInfo.File.IsDescendantOf(packagePart))
            {
                CloseContentViewer();
            }

            NotifyChanges();
        }

        internal void CloseContentViewer()
        {
            ShowContentViewer = false;
            CurrentFileInfo = null;
        }

        public void AddDraggedAndDroppedFiles(PackageFolder folder, string[] fileNames)
        {
            if (folder == null)
            {
                bool? rememberedAnswer = null;

                for (int i = 0; i < fileNames.Length; i++)
                {
                    string file = fileNames[i];
                    if (File.Exists(file))
                    {
                        bool movingFile;

                        PackageFolder targetFolder;
                        string guessFolderName = FileHelper.GuessFolderNameFromFile(file);

                        if (rememberedAnswer == null)
                        {
                            // ask user if he wants to move file
                            Tuple<bool?, bool> answer = UIServices.ConfirmMoveFile(
                                Path.GetFileName(file),
                                guessFolderName, fileNames.Length - i - 1);

                            if (answer.Item1 == null)
                            {
                                // user presses Cancel
                                break;
                            }

                            movingFile = (bool) answer.Item1;
                            if (answer.Item2)
                            {
                                rememberedAnswer = answer.Item1;
                            }
                        }
                        else
                        {
                            movingFile = (bool) rememberedAnswer;
                        }

                        if (movingFile)
                        {
                            if (RootFolder.ContainsFolder(guessFolderName))
                            {
                                targetFolder = (PackageFolder) RootFolder[guessFolderName];
                            }
                            else
                            {
                                targetFolder = RootFolder.AddFolder(guessFolderName);
                            }
                        }
                        else
                        {
                            targetFolder = RootFolder;
                        }

                        targetFolder.AddFile(file, isTempFile: false);
                    }
                    else if (Directory.Exists(file))
                    {
                        RootFolder.AddPhysicalFolder(file);
                    }
                }
            }
            else
            {
                foreach (string file in fileNames)
                {
                    if (File.Exists(file))
                    {
                        folder.AddFile(file, isTempFile: false);
                    }
                    else if (Directory.Exists(file))
                    {
                        folder.AddPhysicalFolder(file);
                    }
                }
            }
        }

        internal bool IsShowingFileContent(PackageFile file)
        {
            return ShowContentViewer && CurrentFileInfo.File == file;
        }

        internal void ShowFileContent(PackageFile file)
        {
            ViewContentCommand.Execute(file);
        }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Globalization", "CA1303:Do not pass literals as localized parameters", MessageId = "NuGetPackageExplorer.Types.IUIServices.ConfirmCloseEditor(System.String,System.String)"), System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Design", "CA1031:DoNotCatchGeneralExceptionTypes")]
        internal bool SaveMetadataAfterEditSource(string editedFilePath)
        {
            if (!File.Exists(editedFilePath))
            {
                return true;
            }

            using (Stream metadataFileStream = File.OpenRead(editedFilePath))
            {
                try
                {
                    Manifest manifest = Manifest.ReadFrom(metadataFileStream, true);
                    var newMetadata = new EditablePackageMetadata(manifest.Metadata, _uiServices);
                    PackageMetadata = newMetadata;

                    return true;
                }
                catch (Exception exception)
                {
                    bool confirmExit = UIServices.ConfirmCloseEditor(
                        "There is an error in the metadata source.", 
                        exception.GetBaseException().Message + 
                        Environment.NewLine +
                        Environment.NewLine +
                        "Do you want to cancel your changes and return?");

                    return confirmExit;
                }
            }
        }
    }
}