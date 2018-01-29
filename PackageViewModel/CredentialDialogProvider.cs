using System;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using NuGet.Configuration;
using NuGet.Credentials;
using NuGetPackageExplorer.Types;
using PackageExplorerViewModel.Types;

namespace PackageExplorerViewModel
{
    public class CredentialDialogProvider : ICredentialProvider
    {
        private readonly Lazy<IUIServices> _uiServices;
        private readonly Lazy<ICredentialManager> _credentialManager;

        public CredentialDialogProvider(Lazy<IUIServices> uIServices, Lazy<ICredentialManager> credentialManager)
        {
            _uiServices = uIServices ?? throw new ArgumentNullException(nameof(uIServices)); 
            _credentialManager = credentialManager ?? throw new ArgumentNullException(nameof(credentialManager)); 
        }

        public string Id => "NPECredentialDialog";

        public async Task<CredentialResponse> GetAsync(Uri uri, IWebProxy proxy, CredentialRequestType type, string message, bool isRetry, bool nonInteractive, CancellationToken cancellationToken)
        {
            //if (nonInteractive)
            //{
            //    return new CredentialResponse(CredentialStatus.ProviderNotApplicable);
            //}

            bool success = false;
            NetworkCredential credential = null;

            await _uiServices.Value.BeginInvoke(() =>
            {
                success = _uiServices.Value.OpenCredentialsDialog(uri.GetLeftPart(UriPartial.Authority), out credential);
            });

            cancellationToken.ThrowIfCancellationRequested();

            if (success)
            {
                _credentialManager.Value.Add(credential, uri);

                return new CredentialResponse(credential);
            }
            return new CredentialResponse(CredentialStatus.UserCanceled);
        }
    }
}
