using System.Diagnostics;
using System.IO;
using System.Windows.Input;
using NuGet;
using NuGetPackageExplorer.Types;

namespace PackageExplorerViewModel
{
    public class FileEditorViewModel : ViewModelBase
    {
        private readonly ICommand _closeCommand;
        private readonly IEditablePackageFile _fileInEdit;
        private readonly string _filePath;
        private readonly PackageViewModel _packageViewModel;
        private readonly ICommand _saveCommand;
        private bool _hasEdit;
        private bool _hasSaved;

        internal FileEditorViewModel(PackageViewModel packageViewModel, IEditablePackageFile fileInEdit)
        {
            Debug.Assert(packageViewModel != null);
            Debug.Assert(fileInEdit != null);

            _packageViewModel = packageViewModel;
            _fileInEdit = fileInEdit;

            // Note: has to preserve the file name here so that the new file "appears" to be the same as old file
            _filePath = fileInEdit.OriginalPath ?? Path.Combine(FileHelper.GetTempFilePath(), fileInEdit.Name);

            _closeCommand = new RelayCommand<IFileEditorService>(CloseExecute);
            _saveCommand = new RelayCommand<IFileEditorService>(SaveExecute);
        }

        public IPackageFile FileInEdit
        {
            get { return _fileInEdit; }
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

        #region CloseCommand

        public ICommand CloseCommand
        {
            get { return _closeCommand; }
        }

        private void CloseExecute(IFileEditorService editorService)
        {
            // if there is unsaved changes, ask user for confirmation
            if (HasEdit)
            {
                bool? result = _packageViewModel.UIServices.ConfirmWithCancel(
                    "You have unsaved changes in the current file.",
                    Resources.Dialog_SaveQuestion);
                if (result == null)
                {
                    return;
                }
                else if (result == true)
                {
                    SaveFile(editorService);
                }
            }

            bool successful = PersitChanges();
            if (successful)
            {
                // return back to Package view
                _packageViewModel.CloseEditFileMode();
            }
        }

        #endregion

        #region SaveCommand

        public ICommand SaveCommand
        {
            get { return _saveCommand; }
        }

        private void SaveExecute(IFileEditorService editorService)
        {
            SaveFile(editorService);
        }

        private void SaveFile(IFileEditorService editorService)
        {
            editorService.Save(_filePath);
            _packageViewModel.NotifyChanges();
            _hasSaved = true;
        }

        #endregion

        public void SaveOnExit(IFileEditorService editorService)
        {
            SaveExecute(editorService);
            PersitChanges();
        }

        private bool PersitChanges()
        {
            if (_hasSaved)
            {
                return _fileInEdit.Save(_filePath);
            }

            return true;
        }
    }
}