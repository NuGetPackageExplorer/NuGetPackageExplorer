using System;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using NuGet.Configuration;
using NuGet.Credentials;
using PackageExplorerViewModel.Types;

namespace PackageExplorerViewModel
{
    public class CredentialManagerProvider : ICredentialProvider
    {
        private readonly Lazy<ICredentialManager> _credentialManager;

        public CredentialManagerProvider(Lazy<ICredentialManager> credentialManager)
        {
            _credentialManager = credentialManager ?? throw new ArgumentNullException(nameof(credentialManager));
        }

        public string Id => "NPECredentialManager";

        public Task<CredentialResponse> GetAsync(Uri uri, IWebProxy proxy, CredentialRequestType type, string message, bool isRetry, bool nonInteractive, CancellationToken cancellationToken)
        {
            if (isRetry)
            {
                return Task.FromResult(new CredentialResponse(CredentialStatus.ProviderNotApplicable));
            }

            var credentials = _credentialManager.Value.Get(uri);

            if (credentials != null)
            {
                return Task.FromResult(new CredentialResponse(credentials));
            }
            return Task.FromResult(new CredentialResponse(CredentialStatus.ProviderNotApplicable));
        }
    }
}
