using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net.NetworkInformation;
using System.Runtime.CompilerServices;
using System.Security.Cryptography.X509Certificates;
using System.Threading.Tasks;
using System.Windows.Input;
using NuGet.Packaging;
using NuGetPackageExplorer.Types;
using NuGetPe;

using LazyPackageCommand = System.Lazy<NuGetPackageExplorer.Types.IPackageCommand, NuGetPackageExplorer.Types.IPackageCommandMetadata>;

namespace PackageExplorerViewModel
{
    public sealed class PackageViewModel : ViewModelBase, IDisposable
    {
        private readonly IPackageEditorService _editorService;
        private readonly IMruManager _mruManager;
        private EditablePackageMetadata _packageMetadata;
        private readonly IList<Lazy<IPackageRule>> _packageRules;
        private readonly CredentialPublishProvider _credentialPublishProvider;

        private ICommand? _addContentFileCommand;
        private ICommand? _addContentFolderCommand;
        private ICommand? _addNewFileCommand;
        private ICommand? _addNewFolderCommand;
        private ICommand? _addScriptCommand;
        private ICommand? _addBuildFileCommand;
        private ICommand? _applyEditCommand;
        private ICommand? _cancelEditCommand;
        private FileContentInfo? _currentFileInfo;
        private RelayCommand<object>? _deleteContentCommand;
        private ICommand? _editCommand;
        private ICommand? _editFileCommand;
        private ICommand? _editMetadataSourceCommand;
        private ICommand? _executePackageCommand;
        private ICommand? _expandAllCommand;
        private ICommand? _collapseAllCommand;
        private RelayCommand? _exportCommand;
        private FileEditorViewModel? _fileEditorViewModel;
        private SymbolValidatorResultViewModel? _symbolValidatorResultViewModel;
        private bool _hasEdit;
        private bool _isInEditMode;
        private RelayCommand<object>? _openContentFileCommand;
        private ICommand? _openWithContentFileCommand;
        private string _packagePath;
        private string _packageSource;
        private RelayCommand? _publishCommand;
        private RelayCommand<object>? _renameContentCommand;
        private SavePackageCommand? _saveCommand;
        private ICommand? _saveContentCommand;
        private object? _selectedItem;
        private bool _showContentViewer;
        private bool _showPackageAnalysis;
        private ICommand? _viewContentCommand;
        private ICommand? _viewPackageAnalysisCommand;
        private ICommand? _removeSignatureCommand;
        private FileSystemWatcher? _watcher;
        private readonly bool _initialized;

#pragma warning disable CS8618 // Non-nullable field is uninitialized.
        internal PackageViewModel(
#pragma warning restore CS8618 // Non-nullable field is uninitialized.
            IPackage package,
            string path,
            string source,
            IMruManager mruManager,
            IUIServices uiServices,
            IPackageEditorService editorService,
            ISettingsManager settingsManager,
            CredentialPublishProvider credentialPublishProvider,
            IList<Lazy<IPackageContentViewer, IPackageContentViewerMetadata>> contentViewerMetadata,
            IList<Lazy<IPackageRule>> packageRules
            )
        {
            SettingsManager = settingsManager ?? throw new ArgumentNullException(nameof(settingsManager));
            _editorService = editorService ?? throw new ArgumentNullException(nameof(editorService));
            UIServices = uiServices ?? throw new ArgumentNullException(nameof(uiServices));
            _mruManager = mruManager ?? throw new ArgumentNullException(nameof(mruManager));
            _credentialPublishProvider = credentialPublishProvider ?? throw new ArgumentNullException(nameof(credentialPublishProvider));
            Package = package ?? throw new ArgumentNullException(nameof(package));
            ContentViewerMetadata = contentViewerMetadata;
            _packageRules = packageRules;





            RootFolder = PathToTreeConverter.Convert(Package.GetFiles().ToList(), this);
            PackagePath = path; // also sets symbol validator
            PackageSource = source;

            _packageMetadata = new EditablePackageMetadata(Package, UIServices, this);

            _isSigned = _packageMetadata.IsSigned;

            _initialized = true;

            // Forces the symbol validator to kick off
            OnPropertyChanged(null);
        }

        internal IList<Lazy<IPackageContentViewer, IPackageContentViewerMetadata>> ContentViewerMetadata { get; }

        internal IUIServices UIServices { get; }

        internal ISettingsManager SettingsManager { get; }

        public bool IsInEditMetadataMode
        {
            get { return _isInEditMode; }
            private set
            {
                if (_isInEditMode != value)
                {
                    _isInEditMode = value;
                    OnPropertyChanged(nameof(IsInEditMetadataMode));
                    OnPropertyChanged(nameof(IsSignedOrInEditMetadataMode));
                }
            }
        }

        public bool IsSignedOrInEditMetadataMode
        {
            get { return IsInEditMetadataMode || IsSigned; }
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
                    OnPropertyChanged(nameof(IsSignedOrInEditMetadataMode));
                    _saveCommand?.RaiseCanExecuteChangedEvent();
                }
            }
        }


        public FileEditorViewModel? FileEditorViewModel
        {
            get { return _fileEditorViewModel; }
            set
            {
                if (_fileEditorViewModel != value)
                {
                    _fileEditorViewModel = value;
                    OnPropertyChanged(nameof(FileEditorViewModel));
                    OnPropertyChanged(nameof(IsInEditFileMode));
                }
            }
        }

        public SymbolValidator SymbolValidator { get; private set; }

        public SymbolValidatorResultViewModel? SymbolValidatorResultViewModel
        {
            get { return _symbolValidatorResultViewModel; }
            set
            {
                if (_symbolValidatorResultViewModel != value)
                {
                    _symbolValidatorResultViewModel = value;
                    OnPropertyChanged(nameof(SymbolValidatorResultViewModel));
                }
            }
        }

        public bool PublishedOnNuGetOrg => SymbolValidator!.IsPublicPackage;

        public IPackage Package { get; }

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
                    OnPropertyChanged(nameof(PackageMetadata));
                    OnPropertyChanged(nameof(IsTokenized));
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
                    OnPropertyChanged(nameof(ShowContentViewer));
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
                    OnPropertyChanged(nameof(ShowPackageAnalysis));
                }
            }
        }

        public FileContentInfo? CurrentFileInfo
        {
            get { return _currentFileInfo; }
            set
            {
                if (_currentFileInfo != value)
                {
                    _currentFileInfo = value;
                    OnPropertyChanged(nameof(CurrentFileInfo));
                }
            }
        }

        public ICollection<PackagePart> PackageParts
        {
            get { return RootFolder.Children; }
        }

        public object? SelectedItem
        {
            get { return _selectedItem; }
            set
            {
                if (_selectedItem != value)
                {
                    _selectedItem = value;
                    OnPropertyChanged(nameof(SelectedItem));
                    ((ViewContentCommand)ViewContentCommand).RaiseCanExecuteChanged();
#if WINDOWS
                    CommandManager.InvalidateRequerySuggested();
#endif
                }
            }
        }

        public string PackagePath
        {
            get { return _packagePath; }
            set
            {
                if (_packagePath != value)
                {
                    _packagePath = value;

                    SymbolValidator = new SymbolValidator(Package, PackagePath, RootFolder);
                    OnPropertyChanged(nameof(PackageSource));
                    OnPropertyChanged(nameof(SymbolValidator));

                    // This may be a URI or a file
                    if (Uri.TryCreate(value, UriKind.Absolute, out var result))
                    {
                        if (result!.IsFile && File.Exists(value))
                        {
#if !NETSTANDARD2_1 // UNO TODO: Use proper platform detection
                            // Clean up the old one since we can't reliably change the Filter without a race
                            if (_watcher != null)
                            {
                                _watcher.EnableRaisingEvents = false;
                                _watcher.Changed -= OnFileChange;
                                _watcher.Deleted -= OnFileChange;
                                _watcher.Renamed -= OnFileChange;
                                _watcher.Dispose();
                                _watcher = null;
                            }

                            _watcher = new FileSystemWatcher();
                            _watcher.Changed += OnFileChange;
                            _watcher.Deleted += OnFileChange;
                            _watcher.Renamed += OnFileChange;

                            _watcher.Path = Path.GetDirectoryName(PackagePath)!;
                            _watcher.Filter = Path.GetFileName(PackagePath);
                            _watcher.EnableRaisingEvents = true;
#endif
                        }
                    }
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
                    OnPropertyChanged();
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
                    OnPropertyChanged(nameof(HasEdit));
                }
            }
        }

        public bool HasFileChangedExternally { get; private set; }

        public ObservableCollection<PackageIssue> PackageIssues { get; } = new ObservableCollection<PackageIssue>();

        public PackageFolder RootFolder { get; }

        public IEnumerable<string> IconPaths
        {
            get
            {
                yield return "";

                foreach (var file in RootFolder.GetFiles())
                {
                    // https://github.com/NuGet/NuGet.Client/blob/1919a94220568927813bde48f71427b5df1fbe92/src/NuGet.Core/NuGet.Packaging/PackageCreation/Authoring/PackageBuilder.cs#L748-L751
                    if (file.Path.EndsWith(".png", StringComparison.OrdinalIgnoreCase) ||
                        file.Path.EndsWith(".jpg", StringComparison.OrdinalIgnoreCase) ||
                        file.Path.EndsWith(".jpeg", StringComparison.OrdinalIgnoreCase))
                    {
                        yield return file.Path;
                    }
                }
            }
        }

        public IEnumerable<string> ReadmePaths
        {
            get
            {
                yield return "";

                foreach (var file in RootFolder.GetFiles())
                {
                    // https://github.com/NuGet/NuGet.Client/blob/1919a94220568927813bde48f71427b5df1fbe92/src/NuGet.Core/NuGet.Packaging/PackageCreation/Authoring/PackageBuilder.cs#L875
                    if (file.Path.EndsWith(NuGet.Configuration.NuGetConstants.ReadmeExtension, StringComparison.OrdinalIgnoreCase))
                    {
                        yield return file.Path;
                    }
                }
            }
        }

        public bool IsDisposed { get; private set; }

        #region IDisposable Members

        public void Dispose()
        {
            IsDisposed = true;

            RootFolder.Dispose();
            Package.Dispose();

            if (_watcher != null)
            {
                _watcher.Changed -= OnFileChange;
                _watcher.Deleted -= OnFileChange;
                _watcher.Renamed -= OnFileChange;
                _watcher.Dispose();
                _watcher = null;
            }
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

        private bool AddContentFileCanExecute(object? parameter)
        {
            if (IsSigned || IsInEditFileMode)
            {
                return false;
            }

            parameter ??= SelectedItem;
            return parameter == null || parameter is PackageFolder;
        }

        private void AddContentFileExecute(object parameter)
        {
            DiagnosticsClient.TrackEvent("PackageViewModel_AddContentFileExecute");

            try
            {
                var folder = (parameter ?? SelectedItem) as PackageFolder;
                AddExistingFileToFolder(folder ?? RootFolder);
            }
            catch (Exception e)
            {
                UIServices.Show(e.Message, MessageLevel.Error);
            }

        }

        [SuppressMessage("Reliability", "CA2000:Dispose objects before losing scope", Justification = "<Pending>")]
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

        [SuppressMessage("Reliability", "CA2000:Dispose objects before losing scope", Justification = "<Pending>")]
        private void AddContentFolderExecute(string folderName)
        {
            DiagnosticsClient.TrackEvent("PackageViewModel_AddContentFolderExecute");

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

        private bool AddNewFolderCanExecute(object? parameter)
        {
            if (IsSigned || IsInEditFileMode)
            {
                return false;
            }

            parameter ??= SelectedItem;
            return parameter == null || parameter is PackageFolder;
        }

        [SuppressMessage("Reliability", "CA2000:Dispose objects before losing scope", Justification = "<Pending>")]
        private void AddNewFolderExecute(object parameter)
        {
            DiagnosticsClient.TrackEvent("PackageViewModel_AddNewFolderExecute");

            parameter ??= SelectedItem ?? RootFolder;
            var folder = parameter as PackageFolder;
            var folderName = "NewFolder";
            var result = UIServices.OpenRenameDialog(folderName, "Provide name for the new folder.", out folderName);

            if (result)
            {
                folder?.AddFolder(folderName);
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
            DiagnosticsClient.TrackEvent("PackageViewModel_EditPackageExecute");

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
            DiagnosticsClient.TrackEvent("PackageViewModel_ApplyEditExecute");

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
            DiagnosticsClient.TrackEvent("PackageViewModel_CancelEditExecute");

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
            DiagnosticsClient.TrackEvent("PackageViewModel_DeleteContentExecute");

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
            DiagnosticsClient.TrackEvent("PackageViewModel_RenameContentExecuted");

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

        private bool OpenContentFileCanExecute(object? parameter)
        {
            if (IsInEditFileMode)
            {
                return false;
            }

            parameter ??= SelectedItem;
            return parameter is PackageFile;
        }

        private void OpenContentFileExecute(object? parameter)
        {
            DiagnosticsClient.TrackEvent("PackageViewModel_OpenContentFileExecute");

            try
            {
                parameter ??= SelectedItem;
                if (parameter is PackageFile file)
                {
                    FileHelper.OpenFileInShell(file, UIServices);
                }
            }
            catch (Exception e)
            {
                UIServices.Show(e.Message, MessageLevel.Error);
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
            DiagnosticsClient.TrackEvent("PackageViewModel_SaveContentExecute");

            try
            {
                var title = "Save " + file.Name;
                const string filter = "All files (*.*)|*.*";
                if (UIServices.OpenSaveFileDialog(title, file.Name, /* initial directory */ null, filter, /* overwritePrompt */ true,
                                                  out var selectedFileName, out var filterIndex))
                {
                    {
                        using var fileStream = File.Open(selectedFileName, FileMode.Create, FileAccess.Write, FileShare.Read);
                        using var packageStream = file.GetStream();
                        packageStream.CopyTo(fileStream);
                    }
                    File.SetLastWriteTime(selectedFileName, file.LastWriteTime.DateTime);
                }
            }
            catch (Exception e)
            {
                UIServices.Show(e.Message, MessageLevel.Error);
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
            DiagnosticsClient.TrackEvent("PackageViewModel_PublishExecute");

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

            try
            {
                using var mruSourceManager = new MruPackageSourceManager(new PublishSourceSettings(SettingsManager));
                using var publishPackageViewModel = new PublishPackageViewModel(
                                                            mruSourceManager,
                                                            SettingsManager,
                                                            UIServices,
                                                            _credentialPublishProvider,
                                                            this);
                UIServices.OpenPublishDialog(publishPackageViewModel);
            }
            catch (Exception e)
            {
                UIServices.Show(e.Message, MessageLevel.Error);
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

        private void ExportExecute()
        {
            DiagnosticsClient.TrackEvent("PackageViewModel_ExportExecute");

            if (UIServices.OpenFolderDialog("Choose a folder to export package to:", _folderPath, out var rootPath))
            {
                try
                {
                    Export(rootPath);
                    UIServices.Show(Resources.ExportPackageSuccess, MessageLevel.Information);
                }
                catch (Exception ex)
                {
                    if (!(ex is IOException) && !(ex is ArgumentException) && !(ex is UnauthorizedAccessException))
                    {
                        DiagnosticsClient.TrackException(ex, Package, PublishedOnNuGetOrg);
                    }
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

        private void PackageCommandExecute(LazyPackageCommand packageCommand)
        {
            var package = PackageHelper.BuildPackage(PackageMetadata, GetFiles());
            try
            {
                packageCommand.Value.Execute(package, PackagePath);
            }
            catch (Exception ex)
            {
                DiagnosticsClient.TrackException(ex, Package, PublishedOnNuGetOrg);
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
                DiagnosticsClient.TrackEvent("PackageViewModel_ViewPackageAnalysisExecute");

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
            DiagnosticsClient.TrackEvent("PackageViewModel_EditFileCommandExecute");

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

            if (file is PackageFile f)
            {
                FileEditorViewModel = new FileEditorViewModel(this, f, UIServices);
            }
            else
            {
                FileEditorViewModel = null;
            }
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
            DiagnosticsClient.TrackEvent("PackageViewModel_RemoveSignatureCommandExecute");

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
            DiagnosticsClient.TrackEvent("PackageViewModel_EditMetadataSourceCommandExecute");

            FileEditorViewModel = new FileEditorViewModel(this, CreatePackageMetadataFile(), UIServices);
        }

        private bool CanEditMetadataSourceCommandExecute()
        {
            return !IsInEditFileMode && !IsInEditMetadataMode;
        }

        public IEditablePackageFile CreatePackageMetadataFile()
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

        private bool AddNewFileCanExecute(object? parameter)
        {
            if (IsSigned || IsInEditFileMode)
            {
                return false;
            }

            parameter ??= SelectedItem;
            return parameter == null || parameter is PackageFolder;
        }

        private void AddNewFileExecute(object parameter)
        {
            DiagnosticsClient.TrackEvent("PackageViewModel_AddNewFileExecute");

            var folder = (parameter ?? SelectedItem) as PackageFolder;
            AddNewFileToFolder(folder ?? RootFolder);
        }

        [SuppressMessage("Reliability", "CA2000:Dispose objects before losing scope", Justification = "<Pending>")]
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

        [SuppressMessage("Reliability", "CA2000:Dispose objects before losing scope", Justification = "<Pending>")]
        private void AddScriptCommandExecute(string scriptName)
        {
            DiagnosticsClient.TrackEvent("PackageViewModel_AddScriptCommandExecute");

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
            if (IsSigned || IsInEditFileMode)
            {
                return false;
            }

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
            if (IsSigned || IsInEditFileMode)
            {
                return false;
            }

            if (SelectedItem is PackageFolder selectedFolder)
            {
                var fileName = PackageMetadata.Id + extension;
                return !selectedFolder.ContainsFile(fileName);
            }

            return false;
        }

        [SuppressMessage("Reliability", "CA2000:Dispose objects before losing scope", Justification = "<Pending>")]
        private void AddBuildFileCommandExecute(string extension)
        {
            DiagnosticsClient.TrackEvent("PackageViewModel_AddBuildFileCommandExecute");

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

        #region ExpandAllCommand

        public ICommand ExpandAllCommand => _expandAllCommand ??= new RelayCommand(ExpandAllCommandExecute, ExpandAllCommandCanExecute);

        private bool ExpandAllCommandCanExecute()
        {
            return PackageParts.Count != 0;
        }

        private void ExpandAllCommandExecute()
        {
            foreach (var folder in RootFolder.GetPackageParts().OfType<PackageFolder>())
            {
                folder.IsExpanded = true;
            }
        }

        #endregion

        #region CollapseAllCommand

        public ICommand CollapseAllCommand => _collapseAllCommand ??= new RelayCommand(CollapseAllCommandExecute, CollapseAllCommandCanExecute);

        private bool CollapseAllCommandCanExecute()
        {
            return PackageParts.Count != 0;
        }

        private void CollapseAllCommandExecute()
        {
            foreach (var folder in RootFolder.GetPackageParts().OfType<PackageFolder>())
            {
                folder.IsExpanded = false;
            }
        }

        #endregion

        private void OnFileChange(object sender, FileSystemEventArgs e)
        {
            HasFileChangedExternally = true;
        }

        private void SetPackageIssues(IEnumerable<PackageIssue> issues)
        {
            PackageIssues.Clear();
            PackageIssues.AddRange(issues);
        }

        public void ShowFile(FileContentInfo fileInfo)
        {
            ShowContentViewer = true;
            CurrentFileInfo = fileInfo;
        }

        internal IEnumerable<IPackageFile> GetFiles()
        {
            return RootFolder.GetPackageFiles();
        }

        public string? GetCurrentPackageTempFile()
        {
            var tempFile = Path.GetTempFileName();
            try
            {
                // handle signed packages since they cannot be resaved without losing the signature
                if (IsSigned)
                {
                    File.Copy(PackagePath, tempFile, overwrite: true);
                }
                else
                {
                    PackageHelper.SavePackage(PackageMetadata, GetFiles(), tempFile, useTempFile: false);
                }
            }
            catch (Exception e)
            {
                if (!(e is ArgumentException))
                {
                    DiagnosticsClient.TrackException(e, Package, PublishedOnNuGetOrg);
                }
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
            OnPropertyChanged(nameof(PackageMetadata));
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

            _saveCommand?.RaiseCanExecuteChangedEvent();
            _publishCommand?.RaiseCanExecuteChanged();
        }

        internal void OnSaved(string fileName)
        {
            HasEdit = false;
            HasFileChangedExternally = false;
            _mruManager.NotifyFileAdded(PackageMetadata, fileName, PackageType.LocalPackage);
        }

        internal void NotifyChanges()
        {
            HasEdit = true;
            OnPropertyChanged(null); // refresh all
        }

        public IEnumerable<PackageIssue> Validate()
        {
            var package = PackageHelper.BuildPackage(PackageMetadata, GetFiles());
            var packageFileName = Path.IsPathRooted(PackagePath) ? Path.GetFileName(PackagePath)! : string.Empty;
            return package.Validate(_packageRules.Select(r => r.Value), packageFileName);
        }

        private void Export(string rootPath)
        {
            if (rootPath == null)
            {
                throw new ArgumentNullException(nameof(rootPath));
            }

            if (!Directory.Exists(rootPath))
            {
                throw new ArgumentException("Specified directory doesn't exist.");
            }

            // export files
            RootFolder.Export(rootPath);

            // export .nuspec file
            ExportManifest(Path.Combine(rootPath, PackageMetadata.FileName + NuGetPe.Constants.ManifestExtension));
        }


        protected override async void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        {
            base.OnPropertyChanged(propertyName);

            if (!_initialized) return;

            // Refresh the symbol validator
            // No need to refresh for certain properties that don't affect the data
            if (propertyName == nameof(SelectedItem) ||
                propertyName == nameof(CurrentFileInfo) ||
                propertyName == nameof(SymbolValidatorResultViewModel))
                return;

            var result = await Task.Run(async () => await SymbolValidator.Validate().ConfigureAwait(false)).ConfigureAwait(true);
            SymbolValidatorResultViewModel = new SymbolValidatorResultViewModel(result);
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

            DiagnosticsClient.TrackEvent("PackageViewModel_ExportManifest");
            var rootPath = Path.GetDirectoryName(fullpath)!;

            using Stream fileStream = File.Create(fullpath);
            var manifest = Manifest.Create(PackageMetadata);
            if (includeFilesSection)
            {
                var tempPath = Path.GetTempPath();

                manifest.Files.AddRange(RootFolder.GetPackageFiles().Select(
                    f => new ManifestFile
                    {

                        Source = string.IsNullOrEmpty(f.OriginalPath()) || f.OriginalPath()?.StartsWith(tempPath, StringComparison.OrdinalIgnoreCase) == true ? f.Path : PathUtility.RelativePathTo(rootPath, f.OriginalPath()!),
                        Target = f.Path
                    })
                );
            }
            using var ms = new MemoryStream();
            try
            {
                manifest.Save(ms);
                ms.Position = 0;
                ManifestUtility.SaveToStream(ms, fileStream);
            }
            catch (Exception e)
            {
                UIServices.Show(e.Message, MessageLevel.Error);
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

        [SuppressMessage("Reliability", "CA2000:Dispose objects before losing scope", Justification = "<Pending>")]
        public void AddDraggedAndDroppedFiles(PackageFolder? folder, string[] fileNames)
        {
            if (fileNames is null)
                throw new ArgumentNullException(nameof(fileNames));

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
                                Path.GetFileName(file)!,
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
                                targetFolder = (PackageFolder)RootFolder[guessFolderName]!;
                            }
                            else
                            {
                                targetFolder = RootFolder.AddFolder(guessFolderName)!;
                            }
                        }
                        else
                        {
                            targetFolder = RootFolder;
                        }

                        targetFolder?.AddFile(file);
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

        [SuppressMessage("Reliability", "CA2000:Dispose objects before losing scope", Justification = "<Pending>")]
        public static void AddDraggedAndDroppedFileDescriptors(PackageFolder folder, IEnumerable<(string FilePath, Stream? Stream)> fileDescriptors)
        {
            if (folder is null)
                throw new ArgumentNullException(nameof(folder));
            if (fileDescriptors is null)
                throw new ArgumentNullException(nameof(fileDescriptors));
            foreach (var fileDescription in fileDescriptors)
            {
                var parts = fileDescription.FilePath.Split(Path.DirectorySeparatorChar);

                var name = parts[^1];
                var parentFolder = folder;
                for (var i = 0; i < parts.Length - 1; i++)
                {
                    parentFolder = (PackageFolder)parentFolder[parts[i]]!;
                    if (parentFolder is null) throw new ArgumentNullException(nameof(folder)); // verify each part isn't null. should never happen
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
            return PackageMetadata.DependencyGroups
                    .SelectMany(ds => ds.Packages)
                    .Any(dp => dp.VersionRange.MinVersion?.IsTokenized() == true || dp.VersionRange.MaxVersion?.IsTokenized() == true);
        }

        public bool IsTokenized => IsPackageTokenized();

        internal bool IsShowingFileContent(PackageFile? file)
        {
            return ShowContentViewer && CurrentFileInfo?.File == file;
        }

        internal void ShowFileContent(PackageFile file)
        {
            ViewContentCommand.Execute(file);
        }

        internal bool SaveMetadataAfterEditSource(string editedFilePath)
        {
            if (!File.Exists(editedFilePath))
            {
                return true;
            }

            using Stream metadataFileStream = File.OpenRead(editedFilePath);
            try
            {
                using var str = ManifestUtility.ReadManifest(metadataFileStream);
                var manifest = Manifest.ReadFrom(str, true);
                var newMetadata = new EditablePackageMetadata(manifest.Metadata, UIServices, this);
                PackageMetadata = newMetadata;

                return true;

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
