using System;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Net;
using NuGet;
using Ookii.Dialogs.Wpf;

namespace PackageExplorer
{
    internal class WindowsCredentialProvider : ICredentialProvider
    {
        #region ICredentialProvider Members

        public ICredentials GetCredentials(Uri uri, IWebProxy proxy, CredentialType credentialType, bool retrying)
        {
            if (uri == null)
            {
                throw new ArgumentNullException("uri");
            }

            return PromptUserForCredentials(uri, forcePrompt: true);
        }

        #endregion

        [SuppressMessage("Microsoft.Security", "CA2122:DoNotIndirectlyExposeMethodsWithLinkDemands")]
        private static ICredentials PromptUserForCredentials(Uri uri, bool forcePrompt)
        {
            string proxyHost = uri.Host;
            string credentialsTarget = string.Format(CultureInfo.InvariantCulture, "PackageExplorer_{0}", proxyHost);

            ICredentials basicCredentials = null;

            using (var dialog = new CredentialDialog())
            {
                dialog.Target = credentialsTarget;
                dialog.WindowTitle = string.Format(
                    CultureInfo.CurrentCulture, Resources.Resources.ProxyConnectToMessage, proxyHost);

                dialog.MainInstruction = dialog.WindowTitle;
                dialog.ShowUIForSavedCredentials = forcePrompt;
                dialog.ShowSaveCheckBox = true;
                if (dialog.ShowDialog())
                {
                    basicCredentials = dialog.Credentials;
                    if (dialog.IsSaveChecked)
                    {
                        CredentialDialog.StoreCredential(credentialsTarget, dialog.Credentials);
                    }
                }
            }
            return basicCredentials;
        }
    }
}