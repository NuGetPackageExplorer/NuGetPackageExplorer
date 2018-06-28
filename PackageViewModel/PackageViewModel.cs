using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net.NetworkInformation;
using System.Windows.Input;
using NuGet.Packaging;
using NuGetPackageExplorer.Types;
using NuGetPe;
using LazyPackageCommand = System.Lazy<NuGetPackageExplorer.Types.IPackageCommand, NuGetPackageExplorer.Types.IPackageCommandMetadata>;

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
        private readonly CredentialPublishProvider _credentialPublishProvider;

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
        private ICommand _removeSignatureCommand;

        #endregion

        internal PackageViewModel(
            IPackage package,
            string source,
            IMruManager mruManager,
            IUIServices uiServices,
            IPackageEditorService editorService,
            ISettingsManager settingsManager,
            CredentialPublishProvider credentialPublishProvider,
            IList<Lazy<IPackageContentViewer, IPackageContentViewerMetadata>> contentViewerMetadata,
            IList<Lazy<IPackageRule>> packageRules)
        {
            _settingsManager = settingsManager ?? throw new ArgumentNullException(nameof(settingsManager));
            _editorService = editorService ?? throw new ArgumentNullException(nameof(editorService));
            _uiServices = uiServices ?? throw new ArgumentNullException(nameof(uiServices));
            _mruManager = mruManager ?? throw new ArgumentNullException(nameof(mruManager));
            _credentialPublishProvider = credentialPublishProvider ?? throw new ArgumentNullException(nameof(credentialPublishProvider));
            _package = package ?? throw new ArgumentNullException(nameof(package));
            _contentViewerMetadata = contentViewerMetadata;
            _packageRules = packageRules;

            _packageMetadata = new EditablePackageMetadata(_package, _uiServices);

            PackageSource = source;

            _isSigned = _packageMetadata.IsSigned;

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

        internal ISettingsManager SettingsManager
        {
            get { return _settingsManager; }
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

        public bool IsSigned
        {
            get { return _isSigned; }
            set
            {
                if (_isSigned != value)
                {
                    _isSigned = value;
                    OnPropertyChanged(nameof(IsSigned));
                    _saveCommand.RaiseCanExecuteChangedEvent();
                }
            }
        }


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
                    OnPropertyChanged("IsTokenized");
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
                    ((ViewContentCommand)ViewContentCommand).RaiseCanExecuteChanged();
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
            if (IsSigned || IsInEditFileMode)
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
            var result = UIServices.OpenMultipleFilesDialog(
                "Select Files",
                "All files (*.*)|*.*",
                out var selectedFiles);

            if (result)
            {
                foreach (var file in selectedFiles)
                {
                    folder.AddFile(file);
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

            if (IsSigned || IsInEditFileMode)
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
            if (IsSigned || IsInEditFileMode)
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
            var folderName = "NewFolder";
            var result = UIServices.OpenRenameDialog(
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
            return !IsSigned && !IsInEditMetadataMode && !IsInEditFileMode;
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
                    _applyEditCommand = new RelayCommand(() => ApplyEditExecute(), () => !IsSigned && !IsInEditFileMode);
                }

                return _applyEditCommand;
            }
        }

        internal bool ApplyEditExecute()
        {
            var valid = _editorService.CommitEdit();
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
                    _cancelEditCommand = new RelayCommand(CancelEditExecute, () => !IsSigned && !IsInEditFileMode);
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
            if (IsSigned || IsInEditFileMode)
            {
                return false;
            }

            return (parameter ?? SelectedItem) is PackagePart;
        }

        private void DeleteContentExecute(object parameter)
        {
            if ((parameter ?? SelectedItem) is PackagePart file)
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
            if (IsSigned || IsInEditFileMode)
            {
                return false;
            }

            return (parameter ?? SelectedItem) is PackagePart;
        }

        private void RenameContentExecuted(object parameter)
        {
            if ((parameter ?? SelectedItem) is PackagePart part)
            {
                var result = UIServices.OpenRenameDialog(
                    part.Name,
                    "Provide new name for '" + part.Name + "'.",
                    out var newName);

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
            if (parameter is PackageFile file)
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
            var title = "Save " + file.Name;
            const string filter = "All files (*.*)|*.*";
            if (UIServices.OpenSaveFileDialog(title, file.Name, /* initial directory */ null, filter, /* overwritePrompt */ true,
                                              out var selectedFileName, out var filterIndex))
            {
                using (var fileStream = File.OpenWrite(selectedFileName))
                {
                    file.GetStream().CopyTo(fileStream);
                }
            }
        }

        private bool SaveContentCanExecute(PackageFile file)
        {
            return !IsInEditFileMode;
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
            var firstIssue = Validate().FirstOrDefault(p => p.Level == PackageIssueLevel.Error);
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
                    _credentialPublishProvider,
                    this);
                _uiServices.OpenPublishDialog(publishPackageViewModel);
            }
        }

        private bool PublishCanExecute()
        {
            return !IsTokenized && !IsInEditMetadataMode && !IsInEditFileMode;
        }

        #endregion

        #region ExportCommand

        private string _folderPath = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
        private bool _isSigned;

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
            if (_uiServices.OpenFolderDialog("Choose a folder to export package to:", _folderPath, out var rootPath))
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
            var package = PackageHelper.BuildPackage(PackageMetadata, GetFiles());
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
                var isMetadataValid = ApplyEditExecute();
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
            return !IsSigned &&
                   (file is PackageFile) &&
                   !IsInEditFileMode &&
                   !FileHelper.IsBinaryFile(file.Path);
        }

        internal void CloseEditFileMode()
        {
            FileEditorViewModel = null;
        }

        #endregion

        #region RemoveSignatureCommand

        public ICommand RemoveSignaturesCommand
        {
            get
            {
                if (_removeSignatureCommand == null)
                {
                    _removeSignatureCommand = new RelayCommand(RemoveSignatureCommandExecute, CanRemoveSignatureCommandExecute);
                }
                return _removeSignatureCommand;
            }
        }

        private void RemoveSignatureCommandExecute()
        {
            // Set this to false, enabling save
            PackageMetadata.ClearSignatures();
            IsSigned = false;
            CommitEdit();
        }

        private bool CanRemoveSignatureCommandExecute()
        {
            return IsSigned;
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
            return !IsSigned && !IsInEditFileMode && !IsInEditMetadataMode;
        }

        private IEditablePackageFile CreatePackageMetadataFile()
        {
            var packageName = PackageMetadata.FileName + NuGetPe.Constants.ManifestExtension;
            var filePath = Path.GetTempFileName();

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
            if (IsSigned || IsInEditFileMode)
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
            var result = UIServices.OpenRenameDialog(
                "NewFile.txt",
                "Provide name for the new file.",
                out var newName);
            if (result)
            {
                var sourcePath = FileHelper.CreateTempFile(newName);
                var file = folder.AddFile(sourcePath);
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
            var content = scriptName.Equals("init.ps1", StringComparison.OrdinalIgnoreCase)
                                 ? Constants.ContentForInit
                                 : Constants.ContentForInstall;
            var sourcePath = FileHelper.CreateTempFile(scriptName, content);

            if (SelectedItem is PackageFolder selectedFolder)
            {
                var file = selectedFolder.AddFile(sourcePath);
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

            return SelectedItem is PackageFolder selectedFolder && !selectedFolder.ContainsFile(scriptName);
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

            if (SelectedItem is PackageFolder selectedFolder)
            {
                var fileName = PackageMetadata.Id + extension;
                return !selectedFolder.ContainsFile(fileName);
            }

            return false;
        }

        private void AddBuildFileCommandExecute(string extension)
        {
            var fileName = PackageMetadata.Id + extension;
            var sourcePath = FileHelper.CreateTempFile(fileName, Constants.ContentForBuildFile);

            if (SelectedItem is PackageFolder selectedFolder)
            {
                var file = selectedFolder.AddFile(sourcePath);
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
            var tempFile = Path.GetTempFileName();
            try
            {
                // handle signed packages since they cannot be resaved without losing the signature
                if (IsSigned && _package is ISignaturePackage zip)
                {
                    File.Copy(zip.Source, tempFile, overwrite: true);
                }
                else
                {
                    PackageHelper.SavePackage(PackageMetadata, GetFiles(), tempFile, useTempFile: false);
                }
            }
            catch (Exception e)
            {
                UIServices.Show(e.Message, MessageLevel.Error);
            }

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

            OnPropertyChanged(nameof(IsTokenized));
            OnPropertyChanged(nameof(WindowTitle));

            _saveCommand.RaiseCanExecuteChangedEvent();
            _publishCommand.RaiseCanExecuteChanged();
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
            var package = PackageHelper.BuildPackage(PackageMetadata, GetFiles());
            var packageFileName = Path.IsPathRooted(PackageSource) ? Path.GetFileName(PackageSource) : null;
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
                var confirmed = UIServices.Confirm(
                    Resources.ConfirmToReplaceFile_Title,
                    string.Format(CultureInfo.CurrentCulture, Resources.ConfirmToReplaceFile, fullpath));
                if (!confirmed)
                {
                    return;
                }
            }

            var rootPath = Path.GetDirectoryName(fullpath);

            using (Stream fileStream = File.Create(fullpath))
            {
                var manifest = Manifest.Create(PackageMetadata);
                if (includeFilesSection)
                {
                    var tempPath = Path.GetTempPath();

                    manifest.Files.AddRange(RootFolder.GetFiles().Select(
                        f => new ManifestFile
                        {
                            Source = string.IsNullOrEmpty(f.OriginalPath()) || f.OriginalPath().StartsWith(tempPath, StringComparison.OrdinalIgnoreCase) ? f.Path : PathUtility.RelativePathTo(rootPath, f.OriginalPath()),
                            Target = f.Path
                        })
                    );
                }
                using (var ms = new MemoryStream())
                {
                    manifest.Save(ms);
                    ms.Position = 0;
                    ManifestUtility.SaveToStream(ms, fileStream);
                }

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

                for (var i = 0; i < fileNames.Length; i++)
                {
                    var file = fileNames[i];
                    if (File.Exists(file))
                    {
                        bool movingFile;

                        PackageFolder targetFolder;
                        var guessFolderName = FileHelper.GuessFolderNameFromFile(file);

                        if (rememberedAnswer == null)
                        {
                            // ask user if he wants to move file
                            var answer = UIServices.ConfirmMoveFile(
                                Path.GetFileName(file),
                                guessFolderName, fileNames.Length - i - 1);

                            if (answer.Item1 == null)
                            {
                                // user presses Cancel
                                break;
                            }

                            movingFile = (bool)answer.Item1;
                            if (answer.Item2)
                            {
                                rememberedAnswer = answer.Item1;
                            }
                        }
                        else
                        {
                            movingFile = (bool)rememberedAnswer;
                        }

                        if (movingFile)
                        {
                            if (RootFolder.ContainsFolder(guessFolderName))
                            {
                                targetFolder = (PackageFolder)RootFolder[guessFolderName];
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

                        targetFolder.AddFile(file);
                    }
                    else if (Directory.Exists(file))
                    {
                        RootFolder.AddPhysicalFolder(file);
                    }
                }
            }
            else
            {
                foreach (var file in fileNames)
                {
                    if (File.Exists(file))
                    {
                        folder.AddFile(file);
                    }
                    else if (Directory.Exists(file))
                    {
                        folder.AddPhysicalFolder(file);
                    }
                }
            }
        }

        public void AddDraggedAndDroppedFileDescriptors(PackageFolder folder, IEnumerable<(string FilePath, Stream Stream)> fileDescriptors)
        {
            foreach (var fileDescription in fileDescriptors)
            {
                var parts = fileDescription.FilePath.Split(Path.DirectorySeparatorChar);

                var name = parts[parts.Length - 1];
                var parentFolder = folder;
                for (var i = 0; i < parts.Length - 1; i++)
                {
                    parentFolder = (PackageFolder)parentFolder[parts[i]];
                }

                if (fileDescription.Stream != null) // file
                {
                    var tempFile = FileHelper.CreateTempFile(name, fileDescription.Stream);

                    parentFolder.AddFile(tempFile);
                }
                else // folder
                {
                    parentFolder.AddFolder(name);
                }
            }
        }

        private bool IsPackageTokenized()
        {
            if (PackageMetadata.Version.IsTokenized())
            {
                return true;
            }

            // any deps
            return PackageMetadata.DependencySets
                    .SelectMany(ds => ds.Packages)
                    .Any(dp => dp.VersionRange.MinVersion?.IsTokenized() == true || dp.VersionRange.MaxVersion?.IsTokenized() == true);
        }

        public bool IsTokenized => IsPackageTokenized();

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
                    using (var str = ManifestUtility.ReadManifest(metadataFileStream))
                    {
                        var manifest = Manifest.ReadFrom(str, true);
                        var newMetadata = new EditablePackageMetadata(manifest.Metadata, _uiServices);
                        PackageMetadata = newMetadata;

                        return true;
                    }

                }
                catch (Exception exception)
                {
                    var confirmExit = UIServices.ConfirmCloseEditor(
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
