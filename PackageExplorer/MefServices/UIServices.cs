using System;
using System.ComponentModel.Composition;
using System.Globalization;
using System.IO;
using System.Net;
using System.Windows;
using System.Windows.Threading;
using Microsoft.Win32;
using NuGetPackageExplorer.Types;
using NuGetPe;
using Ookii.Dialogs.Wpf;

namespace PackageExplorer
{
    [Export(typeof(IUIServices))]
    internal class UIServices : IUIServices
    {
#pragma warning disable CS8618 // Non-nullable field is uninitialized.
        [Import]
        public Lazy<MainWindow> Window { get; set; }
#pragma warning restore CS8618 // Non-nullable field is uninitialized.
        public object Initialize() => Window.Value;

        public bool OpenSaveFileDialog(string title, string defaultFileName, string? initialDirectory, string filter, bool overwritePrompt,
                                       out string selectedFilePath, out int selectedFilterIndex)
        {
            var dialog = new SaveFileDialog
            {
                OverwritePrompt = overwritePrompt,
                Title = title,
                Filter = filter,
                FileName = defaultFileName,
                ValidateNames = true,
                AddExtension = true,
                InitialDirectory = !string.IsNullOrEmpty(initialDirectory) ? Path.GetDirectoryName(initialDirectory) : initialDirectory
            };

            var result = dialog.ShowDialog();
            if (result ?? false)
            {
                selectedFilePath = dialog.FileName;
                selectedFilterIndex = dialog.FilterIndex;
                return true;
            }
            else
            {
                selectedFilePath = string.Empty;
                selectedFilterIndex = -1;
                return false;
            }
        }

        public bool OpenFileDialog(string title, string filter, out string selectedFileName)
        {
            var dialog = new OpenFileDialog
            {
                Title = title,
                CheckFileExists = true,
                CheckPathExists = true,
                FilterIndex = 0,
                Multiselect = false,
                ValidateNames = true,
                Filter = filter
            };

            try
            {
                var result = dialog.ShowDialog();
                if (result ?? false)
                {
                    selectedFileName = dialog.FileName;
                    return true;
                }
                else
                {
                    selectedFileName = string.Empty;
                    return false;
                }
            }
            catch (Exception e)
            {
                Show(e.Message, MessageLevel.Error);
                selectedFileName = string.Empty;
                return false;
            }

        }

        public bool OpenMultipleFilesDialog(string title, string filter, out string[] selectedFileNames)
        {
            var dialog = new OpenFileDialog
            {
                Title = title,
                CheckFileExists = true,
                CheckPathExists = true,
                FilterIndex = 0,
                Multiselect = true,
                ValidateNames = true,
                Filter = filter
            };

            var result = dialog.ShowDialog();
            if (result ?? false)
            {
                selectedFileNames = dialog.FileNames;
                return true;
            }
            else
            {
                selectedFileNames = new string[0];
                return false;
            }
        }

        public bool Confirm(string title, string message)
        {
            return Confirm(title, message, isWarning: false);
        }

        public bool Confirm(string title, string message, bool isWarning)
        {
            return ConfirmUsingTaskDialog(message, title, isWarning);
        }

        public bool? ConfirmWithCancel(string title, string message)
        {
            return ConfirmWithCancelUsingTaskDialog(message, title);
        }

        public bool ConfirmCloseEditor(string title, string message)
        {
            return ConfirmCloseEditorUsingTaskDialog(title, message);
        }

        public void Show(string message, MessageLevel messageLevel)
        {
            MessageBoxImage image;
            switch (messageLevel)
            {
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

            void ShowDialog()
            {
                MessageBox.Show(
                Window.Value,
                message,
                Resources.Dialog_Title,
                MessageBoxButton.OK,
                image);
            }

            if (!Window.Value.Dispatcher.CheckAccess())
            {
                Window.Value.Dispatcher.Invoke(ShowDialog);
            }
            else
            {
                ShowDialog();
            }
        }

        public bool OpenRenameDialog(string currentName, string description, out string newName)
        {
            var dialog = new RenameWindow
            {
                NewName = currentName,
                Description = description,
                Owner = Window.Value
            };

            var result = dialog.ShowDialog();
            if (result ?? false)
            {
                newName = dialog.NewName;
                return true;
            }
            else
            {
                newName = string.Empty;
                return false;
            }
        }

        public bool OpenPublishDialog(object viewModel)
        {
            var dialog = new PublishPackageWindow
            {
                Owner = Window.Value,
                DataContext = viewModel
            };
            if (viewModel is IDisposable)
            {
                dialog.Closed += OnDialogClosed;
            }

            var result = dialog.ShowDialog();
            return result ?? false;
        }

        public bool OpenSignatureValidationDialog(object viewModel)
        {
            var dialog = new ValidationResultWindow
            {
                Owner = Window.Value,
                DataContext = viewModel
            };
            if (viewModel is IDisposable)
            {
                dialog.Closed += OnDialogClosed;
            }

            var result = dialog.ShowDialog();
            return result ?? false;
        }

        public bool OpenSignPackageDialog(object viewModel, out string signedPackagePath)
        {
            var dialog = new SignPackageDialog
            {
                Owner = Window.Value,
                DataContext = viewModel
            };
            if (viewModel is IDisposable)
            {
                dialog.Closed += OnDialogClosed;
            }

            var result = dialog.ShowDialog();
            signedPackagePath = dialog.SignedPackagePath;
            return result ?? false;
        }

        private void OnDialogClosed(object sender, EventArgs e)
        {
            var window = (Window)sender;
            if (window.DataContext is IDisposable disposable)
            {
                disposable.Dispose();
            }
            window.Closed -= OnDialogClosed;
        }

        public bool OpenFolderDialog(string title, string initialPath, out string selectedPath)
        {
            var dialog = new System.Windows.Forms.FolderBrowserDialog()
            {
                SelectedPath = initialPath,
                Description = title,
                UseDescriptionForTitle = true
            };

            try
            {
                var result = dialog.ShowDialog();
                if (result == System.Windows.Forms.DialogResult.OK)
                {
                    selectedPath = dialog.SelectedPath;
                    return true;
                }
                else
                {
                    selectedPath = string.Empty;
                    return false;
                }
            }
            catch(Exception e)
            {
                Show(e.Message, MessageLevel.Error);
                selectedPath = string.Empty;
                return false;
            }
            
        }

        public DispatcherOperation BeginInvoke(Action action)
        {
            return Window.Value.Dispatcher.BeginInvoke(action);
        }

        public Tuple<bool?, bool> ConfirmMoveFile(string fileName, string targetFolder, int numberOfItemsLeft)
        {
            if (numberOfItemsLeft < 0)
            {
                throw new ArgumentOutOfRangeException("numberOfItemsLeft");
            }

            var mainInstruction = string.Format(
                CultureInfo.CurrentCulture,
                Resources.MoveContentFileToFolder,
                fileName,
                targetFolder);

            return ConfirmMoveFileUsingTaskDialog(fileName, targetFolder, numberOfItemsLeft, mainInstruction);
        }

        private static bool ConfirmUsingTaskDialog(string message, string title, bool isWarning)
        {
            using var dialog = new TaskDialog
            {
                WindowTitle = Resources.Dialog_Title,
                MainInstruction = title,
                Content = message,
                AllowDialogCancellation = true,
                CenterParent = true
            };
            //dialog.ButtonStyle = TaskDialogButtonStyle.CommandLinks;
            if (isWarning)
            {
                dialog.MainIcon = TaskDialogIcon.Warning;
            }

            var yesButton = new TaskDialogButton("Yes");
            var noButton = new TaskDialogButton("No");

            dialog.Buttons.Add(yesButton);
            dialog.Buttons.Add(noButton);

            var result = dialog.ShowDialog();
            return result == yesButton;
        }

        private static bool? ConfirmWithCancelUsingTaskDialog(string message, string title)
        {
            using var dialog = new TaskDialog
            {
                WindowTitle = Resources.Dialog_Title,
                MainInstruction = title,
                AllowDialogCancellation = true,
                Content = message,
                CenterParent = true,
                MainIcon = TaskDialogIcon.Warning
            };
            //dialog.ButtonStyle = TaskDialogButtonStyle.CommandLinks;

            var yesButton = new TaskDialogButton("Yes");
            var noButton = new TaskDialogButton("No");
            var cancelButton = new TaskDialogButton("Cancel");

            dialog.Buttons.Add(yesButton);
            dialog.Buttons.Add(noButton);
            dialog.Buttons.Add(cancelButton);

            var result = dialog.ShowDialog();
            if (result == yesButton)
            {
                return true;
            }
            else if (result == noButton)
            {
                return false;
            }

            return null;
        }

        private Tuple<bool?, bool> ConfirmMoveFileUsingTaskDialog(string fileName, string targetFolder,
                                                                  int numberOfItemsLeft, string mainInstruction)
        {
            var content = string.Format(
                CultureInfo.CurrentCulture,
                Resources.MoveContentFileToFolderExplanation,
                targetFolder);

            var dialog = new TaskDialog
            {
                MainInstruction = mainInstruction,
                Content = content,
                WindowTitle = Resources.Dialog_Title,
                ButtonStyle = TaskDialogButtonStyle.CommandLinks
            };

            if (numberOfItemsLeft > 0)
            {
                dialog.VerificationText = "Do this for the next " + numberOfItemsLeft + " file(s).";
            }

            var moveButton = new TaskDialogButton
            {
                Text = "Yes",
                CommandLinkNote =
                                     "'" + fileName + "' will be added to '" + targetFolder +
                                     "' folder."
            };

            var noMoveButton = new TaskDialogButton
            {
                Text = "No",
                CommandLinkNote =
                                       "'" + fileName + "' will be added to the package root."
            };

            dialog.Buttons.Add(moveButton);
            dialog.Buttons.Add(noMoveButton);
            dialog.Buttons.Add(new TaskDialogButton(ButtonType.Cancel));

            var result = dialog.ShowDialog(Window.Value);

            bool? movingFile;
            if (result == moveButton)
            {
                movingFile = true;
            }
            else if (result == noMoveButton)
            {
                movingFile = false;
            }
            else
            {
                // Cancel button clicked
                movingFile = null;
            }

            var remember = dialog.IsVerificationChecked;
            return Tuple.Create(movingFile, remember);
        }

        private static bool ConfirmCloseEditorUsingTaskDialog(string title, string message)
        {
            using var dialog = new TaskDialog
            {
                WindowTitle = Resources.Dialog_Title,
                MainInstruction = title,
                Content = message,
                AllowDialogCancellation = false,
                CenterParent = true,
                ButtonStyle = TaskDialogButtonStyle.CommandLinks
            };

            var yesButton = new TaskDialogButton
            {
                Text = "Yes",
                CommandLinkNote = "Return to package view and lose all your changes."
            };

            var noButton = new TaskDialogButton
            {
                Text = "No",
                CommandLinkNote = "Stay at the metadata editor and fix the error."
            };

            dialog.Buttons.Add(yesButton);
            dialog.Buttons.Add(noButton);

            var result = dialog.ShowDialog();
            return result == yesButton;
        }

        public bool TrySelectPortableFramework(out string portableFramework)
        {
            var dialog = new PortableLibraryDialog
            {
                Owner = Window.Value
            };

            var result = dialog.ShowDialog();
            if (result ?? false)
            {
                portableFramework = dialog.GetSelectedFrameworkName();
                return true;
            }
            else
            {
                portableFramework = string.Empty;
                return false;
            }
        }

        public bool OpenCredentialsDialog(string target, out NetworkCredential? networkCredential)
        {
            DiagnosticsClient.TrackEvent("UIServices_OpenCredentialsDialog");
            using var dialog = new CredentialDialog
            {
                WindowTitle = Resources.Dialog_Title,
                MainInstruction = "Credentials for " + target,
                Content = "Enter Personal Access Tokens in the username field.",
                Target = target
            };

            try
            {
                if (dialog.ShowDialog())
                {
                    networkCredential = dialog.Credentials;
                    return true;
                }
            }
            catch (Exception e)
            {
                Show(e.Message, MessageLevel.Error);
            }

            networkCredential = null;
            return false;
        }
    }
}
