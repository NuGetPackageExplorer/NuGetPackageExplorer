using System;
using System.ComponentModel.Composition;
using System.Globalization;
using System.Runtime.CompilerServices;
using System.Windows;
using Microsoft.Win32;
using NuGetPackageExplorer.Types;
using Ookii.Dialogs.Wpf;

namespace PackageExplorer {

    [Export(typeof(IUIServices))]
    internal class UIServices : IUIServices {

        [Import]
        public Lazy<MainWindow> Window { get; set; }

        private static bool OSSupportsTaskDialogs {
            get {
                return NativeMethods.IsWindowsVistaOrLater;
            }
        }

        public bool OpenSaveFileDialog(string title, string defaultFileName, string filter, out string selectedFilePath) {
            var dialog = new SaveFileDialog() {
                OverwritePrompt = true,
                Title = title,
                Filter = filter,
                FileName = defaultFileName
            };

            bool? result = dialog.ShowDialog();
            if (result ?? false) {
                selectedFilePath = dialog.FileName;
                return true;
            }
            else {
                selectedFilePath = null;
                return false;
            }
        }

        public bool OpenFileDialog(string title, string filter, out string selectedFileName) {
            var dialog = new OpenFileDialog() {
                Title = title,
                CheckFileExists = true,
                CheckPathExists = true,
                FilterIndex = 0,
                Multiselect = false,
                ValidateNames = true,
                Filter = filter
            };

            bool? result = dialog.ShowDialog();
            if (result ?? false) {
                selectedFileName = dialog.FileName;
                return true;
            }
            else {
                selectedFileName = null;
                return false;
            }
        }

        public bool OpenMultipleFilesDialog(string title, string filter, out string[] selectedFileNames) {
            var dialog = new OpenFileDialog() {
                Title = title,
                CheckFileExists = true,
                CheckPathExists = true,
                FilterIndex = 0,
                Multiselect = true,
                ValidateNames = true,
                Filter = filter
            };

            bool? result = dialog.ShowDialog();
            if (result ?? false) {
                selectedFileNames = dialog.FileNames;
                return true;
            }
            else {
                selectedFileNames = null;
                return false;
            }
        }

        public bool Confirm(string title, string message) {
            return Confirm(title, message, isWarning: false);
        }

        public bool Confirm(string title, string message, bool isWarning) {
            if (OSSupportsTaskDialogs) {
                return ConfirmUsingTaskDialog(message, title, isWarning);
            }
            else {
                MessageBoxResult result = MessageBox.Show(
                    Window.Value,
                    message,
                    Resources.Resources.Dialog_Title,
                    MessageBoxButton.YesNo,
                    isWarning ? MessageBoxImage.Warning : MessageBoxImage.Question);
                return result == MessageBoxResult.Yes;
            }
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        private bool ConfirmUsingTaskDialog(string message, string title, bool isWarning) {
            using (TaskDialog dialog = new TaskDialog()) {
                dialog.WindowTitle = Resources.Resources.Dialog_Title;
                dialog.MainInstruction = title;
                dialog.Content = message;
                dialog.CenterParent = true;
                if (isWarning) {
                    dialog.MainIcon = TaskDialogIcon.Warning;
                }

                dialog.Buttons.Add(new TaskDialogButton(ButtonType.Yes));
                dialog.Buttons.Add(new TaskDialogButton(ButtonType.No));

                TaskDialogButton result = dialog.ShowDialog(Window.Value);
                return result.ButtonType == ButtonType.Yes;
            }
        }

        public bool? ConfirmWithCancel(string message, string title) {
            if (OSSupportsTaskDialogs) {
                return ConfirmWithCancelUsingTaskDialog(message, title);
            }
            else {
                MessageBoxResult result = MessageBox.Show(
                    Window.Value,
                    message,
                    Resources.Resources.Dialog_Title,
                    MessageBoxButton.YesNoCancel,
                    MessageBoxImage.Question);
                if (result == MessageBoxResult.Cancel) {
                    return null;
                }
                else {
                    return result == MessageBoxResult.Yes;
                }
            }
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        private bool? ConfirmWithCancelUsingTaskDialog(string message, string title) {
            using (TaskDialog dialog = new TaskDialog()) {
                dialog.WindowTitle = Resources.Resources.Dialog_Title;
                dialog.MainInstruction = title;
                dialog.Content = message;
                dialog.CenterParent = true;
                dialog.MainIcon = TaskDialogIcon.Warning;

                dialog.Buttons.Add(new TaskDialogButton(ButtonType.Yes));
                dialog.Buttons.Add(new TaskDialogButton(ButtonType.No));
                dialog.Buttons.Add(new TaskDialogButton(ButtonType.Cancel));

                TaskDialogButton result = dialog.ShowDialog(Window.Value);
                if (result.ButtonType == ButtonType.Yes) {
                    return true;
                }
                else if (result.ButtonType == ButtonType.No) {
                    return false;
                }
                else {
                    return null;
                }
            }
        }

        public void Show(string message, MessageLevel messageLevel) {
            MessageBoxImage image;
            switch (messageLevel) {
                case MessageLevel.Error:
                    image = MessageBoxImage.Error;
                    break;

                case MessageLevel.Information:
                    image = MessageBoxImage.Information;
                    break;

                case MessageLevel.Warning:
                    image = MessageBoxImage.Warning;
                    break;

                default:
                    throw new ArgumentOutOfRangeException("messageLevel");
            }

            MessageBox.Show(
                Window.Value,
                message,
                Resources.Resources.Dialog_Title,
                MessageBoxButton.OK,
                image);
        }

        public bool OpenRenameDialog(string currentName, string description, out string newName) {
            var dialog = new RenameWindow {
                NewName = currentName,
                Description = description,
                Owner = Window.Value
            };

            bool? result = dialog.ShowDialog();
            if (result ?? false) {
                newName = dialog.NewName;
                return true;
            }
            else {
                newName = null;
                return false;
            }
        }

        public bool OpenPublishDialog(object viewModel) {
            var dialog = new PublishPackageWindow { 
                Owner = Window.Value,
                DataContext = viewModel
            };
            var result = dialog.ShowDialog();
            return result ?? false;
        }

        public bool OpenFolderDialog(string title, string initialPath, out string selectedPath) {
            var dialog = new VistaFolderBrowserDialog() {
                ShowNewFolderButton = true,
                SelectedPath = initialPath,
                Description = title,
                UseDescriptionForTitle = true
            };

            bool? result = dialog.ShowDialog(Window.Value);
            if (result ?? false) {
                selectedPath = dialog.SelectedPath;
                return true;
            }
            else {
                selectedPath = null;
                return false;
            }
        }

        public void BeginInvoke(Action action) {
            Window.Value.Dispatcher.BeginInvoke(action);
        }

        public Tuple<bool?, bool> ConfirmMoveFile(string fileName, string targetFolder, int numberOfItemsLeft) {
            if (numberOfItemsLeft < 0) {
                throw new ArgumentOutOfRangeException("numberofItemsLeft");
            }
            
            string mainInstruction = String.Format(
                CultureInfo.CurrentCulture,
                Resources.Resources.MoveContentFileToFolder,
                fileName,
                targetFolder);

            if (OSSupportsTaskDialogs) {
                return ConfirmMoveFileUsingTaskDialog(fileName, targetFolder, numberOfItemsLeft, mainInstruction);
            }
            else {
                bool? answer = ConfirmWithCancel(mainInstruction, Resources.Resources.Dialog_Title);
                return Tuple.Create(answer, false); 
            }
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        private Tuple<bool?, bool> ConfirmMoveFileUsingTaskDialog(string fileName, string targetFolder, int numberOfItemsLeft, string mainInstruction) {
            string content = String.Format(
                CultureInfo.CurrentCulture,
                Resources.Resources.MoveContentFileToFolderExplanation,
                targetFolder);
            
            TaskDialog dialog = new TaskDialog {
                MainInstruction = mainInstruction,
                Content = content,
                WindowTitle = Resources.Resources.Dialog_Title,
                ButtonStyle = TaskDialogButtonStyle.CommandLinks
            };

            if (numberOfItemsLeft > 0) {
                dialog.VerificationText = "Do this for the next " + numberOfItemsLeft + " file(s).";
            }

            TaskDialogButton moveButton = new TaskDialogButton {
                Text = "Yes",
                CommandLinkNote = "'" + fileName + "' will be added to '" + targetFolder + "' folder."
            };

            TaskDialogButton noMoveButton = new TaskDialogButton {
                Text = "No",
                CommandLinkNote = "'" + fileName + "' will be added to the package root."
            };

            dialog.Buttons.Add(moveButton);
            dialog.Buttons.Add(noMoveButton);
            dialog.Buttons.Add(new TaskDialogButton(ButtonType.Cancel));

            TaskDialogButton result = dialog.ShowDialog(Window.Value);

            bool? movingFile;
            if (result == moveButton) {
                movingFile = true;
            }
            else if (result == noMoveButton) {
                movingFile = false;
            }
            else {
                // Cancel button clicked
                movingFile = null;
            }

            bool remember = dialog.IsVerificationChecked;
            return Tuple.Create(movingFile, remember);
        }
    }
}