using System;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Windows.Input;
using NuGetPackageExplorer.Types;
using NuGetPe;

namespace PackageExplorerViewModel
{
    internal class SavePackageCommand : CommandBase, ICommand
    {
        private const string SaveAction = "Save";
        private const string SaveAsAction = "SaveAs";
        private const string ForceSaveAction = "ForceSave";
        private const string SaveMetadataAction = "SaveMetadataAs";
        private const string SignAndSaveAsAction = "SignAndSaveAs";

        public SavePackageCommand(PackageViewModel model)
            : base(model)
        {
        }

        #region ICommand Members

        public bool CanExecute(object parameter)
        {
            var isSigned = ViewModel.IsSigned;

            var hasTokens = ViewModel.IsTokenized;

            var action = parameter as string;
            if (action == SaveAsAction || action == SaveMetadataAction || action == SignAndSaveAsAction)
            {
                // These actions are allowed since it doesn't modify the file itself
                isSigned = false;
            }

            if (action == SaveMetadataAction)
            {
                // allowed for tokenized things
                hasTokens = false;
            }

            return !isSigned && !hasTokens && !ViewModel.IsInEditFileMode;
        }

        public event EventHandler CanExecuteChanged;

        public void Execute(object parameter)
        {
            if (ViewModel.IsInEditMetadataMode)
            {
                var isMetadataValid = ViewModel.ApplyEditExecute();
                if (!isMetadataValid)
                {
                    ViewModel.UIServices.Show(Resources.EditFormHasInvalidInput, MessageLevel.Error);
                    return;
                }
            }

            try
            {
                var action = parameter as string;
                DiagnosticsClient.TrackEvent($"SavePackageCommand_{action}");

                // if the action is Save Metadata, we don't care if the package is valid
                if (action != SaveMetadataAction)
                {
                    // validate the package to see if there is any error before actually creating the package.
                    var firstIssue =
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
                    if (CanSaveTo(ViewModel.PackageSource) && !ViewModel.HasFileChangedExternally)
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
                else if (action == SignAndSaveAsAction)
                {
                    SignAndSaveAs();
                }
            }
            catch (Exception e)
            {
                ViewModel.UIServices.Show(e.Message, MessageLevel.Error);
            }

        }

        #endregion

        private static bool CanSaveTo(string packageSource)
        {
            return !string.IsNullOrEmpty(packageSource) &&
                   Path.IsPathRooted(packageSource) && (
                   Path.GetExtension(packageSource).Equals(NuGetPe.Constants.PackageExtension, StringComparison.OrdinalIgnoreCase) ||
                   Path.GetExtension(packageSource).Equals(NuGetPe.Constants.SymbolPackageExtension, StringComparison.OrdinalIgnoreCase));
        }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Globalization", "CA1303:Do not pass literals as localized parameters", MessageId = "NuGetPackageExplorer.Types.IUIServices.Confirm(System.String,System.String,System.Boolean)")]
        private void Save()
        {
            var expectedPackageName = ViewModel.PackageMetadata.FileName + NuGetPe.Constants.PackageExtension;
            var expectedSymbolPackageName = ViewModel.PackageMetadata.FileName + NuGetPe.Constants.SymbolPackageExtension;
            var packageName = Path.GetFileName(ViewModel.PackageSource);
            if (!(expectedPackageName.Equals(packageName, StringComparison.OrdinalIgnoreCase) ||
                  expectedSymbolPackageName.Equals(packageName, StringComparison.OrdinalIgnoreCase)))
            {
                var confirmed = ViewModel.UIServices.Confirm(
                    "File name mismatch",
                    "It looks like the package Id and version do not match this file name. Do you still want to save the package as '" + packageName + "'?",
                    isWarning: true);

                if (!confirmed)
                {
                    return;
                }
            }

            var succeeded = SavePackage(ViewModel.PackagePath);
            if (succeeded)
            {
                RaiseCanExecuteChangedEvent();
            }
        }

        private void SaveAs()
        {
            var packageName = ViewModel.PackageMetadata.FileName;
            var title = "Save " + packageName;
            const string filter = "NuGet package file (*.nupkg)|*.nupkg|NuGet Symbols package file (*.snupkg)|*.snupkg|All files (*.*)|*.*";
            var initialDirectory = Path.IsPathRooted(ViewModel.PackageSource) ? ViewModel.PackageSource : null;
            if (ViewModel.UIServices.OpenSaveFileDialog(title, packageName, initialDirectory, filter, /* overwritePrompt */ false,
                                                        out var selectedPackagePath, out var filterIndex))
            {
                if (filterIndex == 1 &&
                    !selectedPackagePath.EndsWith(NuGetPe.Constants.PackageExtension, StringComparison.OrdinalIgnoreCase))
                {
                    selectedPackagePath += NuGetPe.Constants.PackageExtension;
                }
                else if (filterIndex == 2 &&
                         !selectedPackagePath.EndsWith(NuGetPe.Constants.SymbolPackageExtension, StringComparison.OrdinalIgnoreCase))
                {
                    selectedPackagePath += NuGetPe.Constants.SymbolPackageExtension;
                }

                // prompt if the file already exists on disk
                if (File.Exists(selectedPackagePath))
                {
                    var confirmed = ViewModel.UIServices.Confirm(
                        Resources.ConfirmToReplaceFile_Title,
                        string.Format(CultureInfo.CurrentCulture, Resources.ConfirmToReplaceFile, selectedPackagePath));
                    if (!confirmed)
                    {
                        return;
                    }
                }

                var succeeded = SavePackage(selectedPackagePath);
                if (succeeded)
                {
                    ViewModel.PackagePath = selectedPackagePath;
                    ViewModel.PackageSource = selectedPackagePath;
                }
            }
            RaiseCanExecuteChangedEvent();
        }

        [SuppressMessage("Microsoft.Design", "CA1031:DoNotCatchGeneralExceptionTypes")]
        private void SaveMetadataAs()
        {
            var packageName = ViewModel.PackageMetadata.FileName + NuGetPe.Constants.ManifestExtension;
            var title = "Save " + packageName;
            const string filter = "NuGet manifest file (*.nuspec)|*.nuspec|All files (*.*)|*.*";
            var initialDirectory = Path.IsPathRooted(ViewModel.PackageSource) ? ViewModel.PackageSource : null;
            if (ViewModel.UIServices.OpenSaveFileDialog(title, packageName, initialDirectory, filter, /* overwritePrompt */ false,
                                                        out var selectedPath, out var filterIndex))
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
                    DiagnosticsClient.TrackException(ex);
                    ViewModel.UIServices.Show(ex.Message, MessageLevel.Error);
                }
            }
        }

        private async void SignAndSaveAs()
        {
            var signViewModel = new SignPackageViewModel(ViewModel, ViewModel.UIServices, ViewModel.SettingsManager);

            if (ViewModel.UIServices.OpenSignPackageDialog(signViewModel, out var signedPackagePath))
            {
                var packageName = ViewModel.PackageMetadata.FileName;
                var title = "Save " + packageName;
                const string filter = "NuGet package file (*.nupkg)|*.nupkg|NuGet Symbols package file (*.snupkg)|*.snupkg|All files (*.*)|*.*";
                var initialDirectory = Path.IsPathRooted(ViewModel.PackageSource) ? ViewModel.PackageSource : null;
                if (ViewModel.UIServices.OpenSaveFileDialog(title, packageName, initialDirectory, filter, /* overwritePrompt */ false,
                                                            out var selectedPackagePath, out var filterIndex))
                {
                    if (filterIndex == 1 &&
                        !selectedPackagePath.EndsWith(NuGetPe.Constants.PackageExtension, StringComparison.OrdinalIgnoreCase))
                    {
                        selectedPackagePath += NuGetPe.Constants.PackageExtension;
                    }
                    else if (filterIndex == 2 &&
                             !selectedPackagePath.EndsWith(NuGetPe.Constants.SymbolPackageExtension, StringComparison.OrdinalIgnoreCase))
                    {
                        selectedPackagePath += NuGetPe.Constants.SymbolPackageExtension;
                    }

                    // prompt if the file already exists on disk
                    if (File.Exists(selectedPackagePath))
                    {
                        var confirmed = ViewModel.UIServices.Confirm(
                            Resources.ConfirmToReplaceFile_Title,
                            string.Format(CultureInfo.CurrentCulture, Resources.ConfirmToReplaceFile, selectedPackagePath));
                        if (!confirmed)
                        {
                            return;
                        }
                    }

                    try
                    {
                        File.Copy(signedPackagePath, selectedPackagePath, overwrite: true);
                        ViewModel.OnSaved(selectedPackagePath);
                        ViewModel.PackagePath = selectedPackagePath;
                        ViewModel.PackageSource = selectedPackagePath;
                        ViewModel.PackageMetadata.ClearSignatures();
                        using (var package = new ZipPackage(selectedPackagePath))
                        {
                            await package.LoadSignatureDataAsync();

                            ViewModel.PackageMetadata.LoadSignatureData(package);
                        }
                        ViewModel.IsSigned = true;
                    }
                    catch (Exception ex)
                    {
                        DiagnosticsClient.TrackException(ex);
                        ViewModel.UIServices.Show(ex.Message, MessageLevel.Error);
                    }
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
