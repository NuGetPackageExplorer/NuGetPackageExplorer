 using System;
using System.Globalization;
using System.Net;
using NuGet;
using Ookii.Dialogs.Wpf;

namespace PackageExplorer {
    internal class WindowsCredentialProvider : ICredentialProvider {

        public ICredentials GetCredentials(Uri uri, IWebProxy proxy) {
            if (uri == null) {
                throw new ArgumentNullException("uri");
            }

            return PromptUserForCredentials(uri, forcePrompt: true);

            //// Capture the original proxy before we do anything 
            //// so that we can re-set it once we get the credentials for the given Uri.
            //IWebProxy originalProxy = null;
            //if (proxy != null) {
            //    // If the current Uri should be bypassed then don't try to get the specific
            //    // proxy but simply capture the one that is given to us
            //    if (proxy.IsBypassed(uri)) {
            //        originalProxy = proxy;
            //    }
            //    // If the current Uri is not bypassed then get a valid proxy for the Uri
            //    // and make sure that we have the credentials also.
            //    else {
            //        originalProxy = new WebProxy(proxy.GetProxy(uri));
            //        originalProxy.Credentials = proxy.Credentials == null
            //                                        ? null : proxy.Credentials.GetCredential(uri, null);
            //    }
            //}

            //try {
            //    // The cached credentials that we found are not valid so let's ask the user
            //    // until they abort or give us valid credentials.
            //    //InitializeCredentialProxy(uri, originalProxy);

            //    return PromptUserForCredentials(uri, forcePrompt: true);
            //}
            //finally {
            //    // Reset the original WebRequest.DefaultWebProxy to what it was when we started credential discovery.
            //    WebRequest.DefaultWebProxy = originalProxy;
            //}
        }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Security", "CA2122:DoNotIndirectlyExposeMethodsWithLinkDemands")]
        private static ICredentials PromptUserForCredentials(Uri uri, bool forcePrompt) {
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