using System;
using System.Linq;
using System.Net;

namespace NuGet {
    // Default implementation of the Credentials provider which will return nothing
    // by default but can be used as a base class for a more elaborate Proxy Credential Provider
    public class DefaultCredentialProvider : ICredentialProvider {
        public virtual ICredentials DefaultCredentials {
            get { return CredentialCache.DefaultCredentials; }
        }

        public virtual ICredentials[] GetCredentials(Uri uri) {
            return null;
        }

        public virtual ICredentials PromptUserForCredentials(Uri uri, bool retryCredentials) {
            throw new NotImplementedException();
        }
    }
}