using System;
using System.Diagnostics;
using System.IO;
using System.Windows.Input;
using NuGet.Packaging;
using NuGetPackageExplorer.Types;

namespace PackageExplorerViewModel
{
    public class FileEditorViewModel : ViewModelBase
    {
        private readonly IEditablePackageFile _fileInEdit;
        private readonly IUIServices _uiServices;
        private readonly string _filePath;
        private readonly PackageViewModel _packageViewModel;
        private bool _hasEdit;
        private bool _hasSaved;

        internal FileEditorViewModel(PackageViewModel packageViewModel, IEditablePackageFile fileInEdit, IUIServices uiServices)
        {
            Debug.Assert(packageViewModel != null);
            Debug.Assert(fileInEdit != null);

            _packageViewModel = packageViewModel;
            _fileInEdit = fileInEdit;
            _uiServices = uiServices;

            // Note: has to preserve the file name here so that the new file "appears" to be the same as old file
            _filePath = fileInEdit.OriginalPath ?? Path.Combine(FileHelper.GetTempFilePath(), fileInEdit.Name);

            CloseCommand = new RelayCommand<IFileEditorService>(CloseExecute);
            SaveCommand = new RelayCommand<IFileEditorService>(SaveExecute, CanSaveExecute);
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

        public bool IsReadOnly
        {
            get { return _packageViewModel.IsSigned; }
        }


        public ICommand CloseCommand { get; }

        private void CloseExecute(IFileEditorService editorService)
        {
            // if there is unsaved changes, ask user for confirmation
            if (HasEdit)
            {
                var result = _packageViewModel.UIServices.ConfirmWithCancel(
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

            var successful = PersitChanges();
            if (successful)
            {
                // return back to Package view
                _packageViewModel.CloseEditFileMode();
            }
        }

        public ICommand SaveCommand { get; }

        private bool CanSaveExecute(IFileEditorService obj)
        {
            return !IsReadOnly;
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

        public void SaveOnExit(IFileEditorService editorService)
        {
            try
            {
                SaveExecute(editorService);
                PersitChanges();
            }
            catch (Exception e)
            {
                _uiServices.Show(e.Message, MessageLevel.Error);
            }

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
