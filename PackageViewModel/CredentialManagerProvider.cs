using System;
using System.ComponentModel.Composition;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using NuGet.Configuration;
using NuGet.Credentials;
using PackageExplorerViewModel.Types;

namespace PackageExplorerViewModel
{
    [Export]
    public class CredentialManagerProvider : ICredentialProvider
    {
        private readonly ICredentialManager _credentialManager;

        [ImportingConstructor]
        public CredentialManagerProvider(ICredentialManager credentialManager)
        {
            _credentialManager = credentialManager ?? throw new ArgumentNullException(nameof(credentialManager));
        }

        public string Id => "NPECredentialManager";

        public Task<CredentialResponse> GetAsync(Uri uri, IWebProxy proxy, CredentialRequestType type, string message, bool isRetry, bool nonInteractive, CancellationToken cancellationToken)
        {
            if (isRetry)
            {
                return Task.FromResult(new CredentialResponse(CredentialStatus.UserCanceled));
            }

            var credentials = _credentialManager.Get(uri);

            if (credentials != null)
            {
                return Task.FromResult(new CredentialResponse(credentials));
            }
            return Task.FromResult(new CredentialResponse(CredentialStatus.UserCanceled));
        }
    }
}
