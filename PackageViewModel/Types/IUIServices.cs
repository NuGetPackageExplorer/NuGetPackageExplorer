using System;

namespace NuGetPackageExplorer.Types {

    public enum MessageLevel {
        Information,
        Warning,
        Error
    }

    public interface IUIServices {
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Design", "CA1021:AvoidOutParameters", MessageId = "3#")]
        bool OpenSaveFileDialog(string title, string defaultFileName, string filter, out string selectedFilePath);
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Design", "CA1021:AvoidOutParameters", MessageId = "2#")]
        bool OpenFileDialog(string title, string filter, out string selectedFileName);
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Design", "CA1021:AvoidOutParameters", MessageId = "2#")]
        bool OpenMultipleFilesDialog(string title, string filter, out string[] selectedFileNames);

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Design", "CA1021:AvoidOutParameters", MessageId = "2#")]
        bool OpenRenameDialog(string currentName, string description, out string newName);

        bool OpenPublishDialog(object viewModel);
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Design", "CA1021:AvoidOutParameters", MessageId = "2#")]
        bool OpenFolderDialog(string title, string initialPath, out string selectedPath);

        bool Confirm(string title, string message);
        bool Confirm(string title, string message, bool isWarning);
        bool? ConfirmWithCancel(string message, string title);
        void Show(string message, MessageLevel messageLevel);

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Design", "CA1006:DoNotNestGenericTypesInMemberSignatures")]
        Tuple<bool?, bool> ConfirmMoveFile(string fileName, string targetFolder, int numberOfItemsLeft);

        void BeginInvoke(Action action);
    }
}