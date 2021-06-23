using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Net;
using System.Text;
using System.Threading.Tasks;

using Microsoft.Extensions.Logging;

using NuGetPackageExplorer.Types;

using Uno.Extensions;
using Uno.Logging;

namespace NuGetPackageExplorer.MefServices
{
    [Export(typeof(IUIServices))]
    internal class UIServices : IUIServices
    {
        public Task BeginInvoke(Action action)
        {
            throw new NotImplementedException();
        }

        public bool Confirm(string title, string message)
        {
            throw new NotImplementedException();
        }

        public bool Confirm(string title, string message, bool isWarning)
        {
            throw new NotImplementedException();
        }

        public bool ConfirmCloseEditor(string title, string message)
        {
            throw new NotImplementedException();
        }

        public Tuple<bool?, bool> ConfirmMoveFile(string fileName, string targetFolder, int numberOfItemsLeft)
        {
            throw new NotImplementedException();
        }

        public bool? ConfirmWithCancel(string title, string message)
        {
            throw new NotImplementedException();
        }

        public object Initialize()
        {
            return null;
        }

        public bool OpenCredentialsDialog(string target, out NetworkCredential? networkCredential)
        {
            throw new NotImplementedException();
        }

        public bool OpenFileDialog(string title, string filter, out string selectedFileName)
        {
            throw new NotImplementedException();
        }

        public bool OpenFolderDialog(string title, string initialPath, out string selectedPath)
        {
            throw new NotImplementedException();
        }

        public bool OpenMultipleFilesDialog(string title, string filter, out string[] selectedFileNames)
        {
            throw new NotImplementedException();
        }

        public bool OpenPublishDialog(object viewModel)
        {
            throw new NotImplementedException();
        }

        public bool OpenRenameDialog(string currentName, string description, out string newName)
        {
            throw new NotImplementedException();
        }

        public bool OpenSaveFileDialog(string title, string defaultFileName, string? initialDirectory, string filter, bool overwritePrompt, out string selectedFilePath, out int selectedFilterIndex)
        {
            throw new NotImplementedException();
        }

        public bool OpenSignatureValidationDialog(object viewModel)
        {
            throw new NotImplementedException();
        }

        public bool OpenSignPackageDialog(object viewModel, out string signedPackagePath)
        {
            throw new NotImplementedException();
        }

        public void Show(string message, MessageLevel messageLevel)
        {
            var level = messageLevel switch
            {
                MessageLevel.Error => LogLevel.Error,
                MessageLevel.Warning => LogLevel.Warning,
                MessageLevel.Information => LogLevel.Information,

                _ => LogLevel.None,
            };
            this.Log().Log(level, message);
            throw new NotImplementedException();
        }

        public bool TrySelectPortableFramework(out string portableFramework)
        {
            throw new NotImplementedException();
        }
    }
}
