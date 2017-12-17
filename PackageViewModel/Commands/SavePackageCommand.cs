using System;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Windows.Input;
using NuGetPackageExplorer.Types;

namespace PackageExplorerViewModel
{
    internal class SavePackageCommand : CommandBase, ICommand
    {
        private const string SaveAction = "Save";
        private const string SaveAsAction = "SaveAs";
        private const string ForceSaveAction = "ForceSave";
        private const string SaveMetadataAction = "SaveMetadataAs";

        public SavePackageCommand(PackageViewModel model)
            : base(model)
        {
        }

        #region ICommand Members

        public bool CanExecute(object parameter)
        {
            var isSigned = ViewModel.IsSigned;

            var action = parameter as string;
            if (action == SaveAsAction || action == SaveMetadataAction)
            {
                // These actions are allowed since it doesn't modify the file itself
                isSigned = false;    
            }


            return !isSigned && !ViewModel.IsInEditFileMode;
        }

        public event EventHandler CanExecuteChanged;

        public void Execute(object parameter)
        {
            if (ViewModel.IsInEditMetadataMode)
            {
                bool isMetadataValid = ViewModel.ApplyEditExecute();
                if (!isMetadataValid)
                {
                    ViewModel.UIServices.Show(Resources.EditFormHasInvalidInput, MessageLevel.Error);
                    return;
                }
            }

            var action = parameter as string;

            // if the action is Save Metadata, we don't care if the package is valid
            if (action != SaveMetadataAction)
            {
                // validate the package to see if there is any error before actually creating the package.
                PackageIssue firstIssue =
                    ViewModel.Validate().FirstOrDefault(p => p.Level == PackageIssueLevel.Error);
                if (firstIssue != null)
                {
                    ViewModel.UIServices.Show(
                        Resources.PackageCreationFailed
                        + Environment.NewLine
                        + Environment.NewLine
                        + firstIssue.Description,
                        MessageLevel.Warning);
                    return;
                }
            }

            if (action == SaveAction || action == ForceSaveAction)
            {
                if (CanSaveTo(ViewModel.PackageSource))
                {
                    Save();
                }
                else
                {
                    SaveAs();
                }
            }
            else if (action == SaveAsAction)
            {
                SaveAs();
            }
            else if (action == SaveMetadataAction)
            {
                SaveMetadataAs();
            }
        }

        #endregion

        private static bool CanSaveTo(string packageSource)
        {
            return !String.IsNullOrEmpty(packageSource) &&
                   Path.IsPathRooted(packageSource) &&
                   Path.GetExtension(packageSource).Equals(NuGetPe.Constants.PackageExtension,
                                                           StringComparison.OrdinalIgnoreCase);
        }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Globalization", "CA1303:Do not pass literals as localized parameters", MessageId = "NuGetPackageExplorer.Types.IUIServices.Confirm(System.String,System.String,System.Boolean)")]
        private void Save()
        {
            string expectedPackageName = ViewModel.PackageMetadata + NuGetPe.Constants.PackageExtension;
            string packageName = Path.GetFileName(ViewModel.PackageSource);
            if (!expectedPackageName.Equals(packageName, StringComparison.OrdinalIgnoreCase))
            {
                bool confirmed = ViewModel.UIServices.Confirm(
                    "File name mismatch",
                    "It looks like the package Id and version do not match this file name. Do you still want to save the package as '" + packageName + "'?",
                    isWarning: true);

                if (!confirmed)
                {
                    return;
                }
            }

            bool succeeded = SavePackage(ViewModel.PackageSource);
            if (succeeded)
            {
                RaiseCanExecuteChangedEvent();
            }
        }

        private void SaveAs()
        {
            string packageName = ViewModel.PackageMetadata + NuGetPe.Constants.PackageExtension;
            string title = "Save " + packageName;
            const string filter = "NuGet package file (*.nupkg)|*.nupkg|All files (*.*)|*.*";
            string selectedPackagePath;
            int filterIndex;
            string initialDirectory = Path.IsPathRooted(ViewModel.PackageSource) ? ViewModel.PackageSource : null;
            if (ViewModel.UIServices.OpenSaveFileDialog(title, packageName, initialDirectory, filter, /* overwritePrompt */ false,
                                                        out selectedPackagePath, out filterIndex))
            {
                if (filterIndex == 1 &&
                    !selectedPackagePath.EndsWith(NuGetPe.Constants.PackageExtension, StringComparison.OrdinalIgnoreCase))
                {
                    selectedPackagePath += NuGetPe.Constants.PackageExtension;
                }

                // prompt if the file already exists on disk
                if (File.Exists(selectedPackagePath))
                {
                    bool confirmed = ViewModel.UIServices.Confirm(
                        Resources.ConfirmToReplaceFile_Title,
                        String.Format(CultureInfo.CurrentCulture, Resources.ConfirmToReplaceFile, selectedPackagePath));
                    if (!confirmed)
                    {
                        return;
                    }
                }

                bool succeeded = SavePackage(selectedPackagePath);
                if (succeeded)
                {
                    ViewModel.PackageSource = selectedPackagePath;
                }
            }
            RaiseCanExecuteChangedEvent();
        }

        [SuppressMessage("Microsoft.Design", "CA1031:DoNotCatchGeneralExceptionTypes")]
        private void SaveMetadataAs()
        {
            string packageName = ViewModel.PackageMetadata + NuGetPe.Constants.ManifestExtension;
            string title = "Save " + packageName;
            const string filter = "NuGet manifest file (*.nuspec)|*.nuspec|All files (*.*)|*.*";
            string selectedPath;
            int filterIndex;
            string initialDirectory = Path.IsPathRooted(ViewModel.PackageSource) ? ViewModel.PackageSource : null;
            if (ViewModel.UIServices.OpenSaveFileDialog(title, packageName, initialDirectory, filter, /* overwritePrompt */ false,
                                                        out selectedPath, out filterIndex))
            {
                try
                {
                    if (filterIndex == 1 &&
                        !selectedPath.EndsWith(NuGetPe.Constants.ManifestExtension, StringComparison.OrdinalIgnoreCase))
                    {
                        selectedPath += NuGetPe.Constants.ManifestExtension;
                    }

                    ViewModel.ExportManifest(selectedPath);
                    ViewModel.OnSaved(selectedPath);
                }
                catch (Exception ex)
                {
                    ViewModel.UIServices.Show(ex.Message, MessageLevel.Error);
                }
            }
        }

        [SuppressMessage("Microsoft.Design", "CA1031:DoNotCatchGeneralExceptionTypes")]
        private bool SavePackage(string fileName)
        {
            try
            {
                PackageHelper.SavePackage(ViewModel.PackageMetadata, ViewModel.GetFiles(), fileName, true);
                ViewModel.OnSaved(fileName);
                return true;
            }
            catch (Exception ex)
            {
                ViewModel.UIServices.Show(ex.Message, MessageLevel.Error);
                return false;
            }
        }

        internal void RaiseCanExecuteChangedEvent()
        {
            CanExecuteChanged?.Invoke(this, EventArgs.Empty);
        }
    }
}