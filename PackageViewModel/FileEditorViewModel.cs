using System;
using System.Diagnostics;
using System.IO;
using System.Windows.Input;
using NuGet;
using NuGetPackageExplorer.Types;

namespace PackageExplorerViewModel {
    public class FileEditorViewModel : ViewModelBase {
        private readonly PackageViewModel _packageViewModel;
        private readonly PackageFile _fileInEdit;
        private readonly ICommand _closeCommand, _saveCommand;
        private readonly Lazy<string> _filePath;

        internal FileEditorViewModel(PackageViewModel packageViewModel, PackageFile fileInEdit) {
            Debug.Assert(packageViewModel != null);
            Debug.Assert(fileInEdit != null);
            Debug.Assert(fileInEdit.Parent != null);

            _packageViewModel = packageViewModel;
            _fileInEdit = fileInEdit;

            // Note: has to preseve the file name here so that the new file "appears" to be the same as old file
            _filePath = new Lazy<string>(() => Path.Combine(FileHelper.GetTempFilePath(), Path.GetFileName(fileInEdit.Name)));

            _closeCommand = new RelayCommand<IFileEditorService>(CloseExecute);
            _saveCommand = new RelayCommand<IFileEditorService>(SaveExecute);
        }

        public IPackageFile FileInEdit {
            get {
                return _fileInEdit;
            }
        }

        #region CloseCommand

        public ICommand CloseCommand {
            get {
                return _closeCommand;
            }
        }

        private void CloseExecute(IFileEditorService editorService) {
            SaveFile(editorService);

            PersistChangesToPackage();

            // return back to Package view
            _packageViewModel.CloseEditFileMode();
        }

        #endregion

        #region SaveCommand

        public ICommand SaveCommand {
            get {
                return _saveCommand;
            }
        }

        private void SaveExecute(IFileEditorService editorService) {
            SaveFile(editorService);
        }

        #endregion

        private void SaveFile(IFileEditorService editorService) {
            editorService.Save(_filePath.Value);
        }

        private void PersistChangesToPackage() {
            // only persist changes if the file has been saved at least once
            if (_filePath.IsValueCreated) {
                _fileInEdit.Parent.ReplaceFile(_fileInEdit, _filePath.Value);
                _packageViewModel.NotifyChanges();
            }
        }
    }
}