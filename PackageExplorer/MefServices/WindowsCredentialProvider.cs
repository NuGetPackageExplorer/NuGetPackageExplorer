using System;
using System.Globalization;
using System.Net;
using NuGet;
using Ookii.Dialogs.Wpf;

namespace PackageExplorer {
    public class WindowsCredentialProvider : DefaultCredentialProvider {

        public override ICredentials PromptUserForCredentials(Uri uri, bool retryCredentials) {
            return GetBasicCredentials(uri, retryCredentials);
        }

        public override ICredentials[] GetCredentials(Uri uri) {
            if (null == uri) {
                throw new ArgumentNullException("uri");
            }

            var savedCredentials = CredentialDialog.RetrieveCredential(uri.Host);

            return savedCredentials == null ? null : new ICredentials[] {savedCredentials};
        }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Security", "CA2122:DoNotIndirectlyExposeMethodsWithLinkDemands")]
        private static ICredentials GetBasicCredentials(Uri uri, bool forcePrompt) {
            string proxyHost = uri.Host;
            string credentialsTarget = string.Format(CultureInfo.InvariantCulture, "PackageExplorer_{0}", proxyHost);

            ICredentials basicCredentials = null;

            using(var dialog = new CredentialDialog()) {
                dialog.Target = credentialsTarget;
                dialog.WindowTitle = string.Format(CultureInfo.CurrentCulture, Resources.Resources.ProxyConnectToMessage, proxyHost);
                dialog.MainInstruction = dialog.WindowTitle;
                dialog.ShowUIForSavedCredentials = forcePrompt;
                dialog.ShowSaveCheckBox = true;
                if(dialog.ShowDialog()) {
                    basicCredentials = dialog.Credentials;
                    if(dialog.IsSaveChecked) {
                        CredentialDialog.StoreCredential(credentialsTarget, dialog.Credentials);
                    }
                }
            }
            return basicCredentials;
        }
    }
}