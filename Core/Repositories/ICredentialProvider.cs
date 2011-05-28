using System;
using System.Net;

namespace NuGet {
    public interface ICredentialProvider {
        ICredentials[] GetCredentials(Uri uri);
        ICredentials PromptUserForCredentials(Uri uri, bool retryCredentials);
        ICredentials DefaultCredentials { get; }
    }
}