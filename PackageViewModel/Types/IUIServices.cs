using System;
using System.Diagnostics.CodeAnalysis;
using System.Net;
using System.Windows.Threading;

namespace NuGetPackageExplorer.Types
{
    public enum MessageLevel
    {
        Information,
        Warning,
        Error
    }

    public interface IUIServices
    {
        object Initialize();

        bool OpenSaveFileDialog(string title, string defaultFileName, string? initialDirectory, string filter, bool overwritePrompt,
                                out string selectedFilePath, out int selectedFilterIndex);

        bool OpenFileDialog(string title, string filter, out string selectedFileName);
        bool OpenMultipleFilesDialog(string title, string filter, out string[] selectedFileNames);

        bool OpenRenameDialog(string currentName, string description, out string newName);

        bool OpenCredentialsDialog(string target, out NetworkCredential? networkCredential);

        bool OpenPublishDialog(object viewModel);
        bool OpenSignatureValidationDialog(object viewModel);
        bool OpenSignPackageDialog(object viewModel, out string signedPackagePath);
        bool OpenFolderDialog(string title, string initialPath, out string selectedPath);

        bool Confirm(string title, string message);
        bool Confirm(string title, string message, bool isWarning);
        bool? ConfirmWithCancel(string title, string message);
        void Show(string message, MessageLevel messageLevel);

        [SuppressMessage("Microsoft.Design", "CA1006:DoNotNestGenericTypesInMemberSignatures")]
        Tuple<bool?, bool> ConfirmMoveFile(string fileName, string targetFolder, int numberOfItemsLeft);

        bool TrySelectPortableFramework(out string portableFramework);

        bool ConfirmCloseEditor(string title, string message);

        DispatcherOperation BeginInvoke(Action action);
    }
}
