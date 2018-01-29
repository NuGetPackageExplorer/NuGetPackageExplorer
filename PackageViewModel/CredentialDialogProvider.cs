using System;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using NuGet.Configuration;
using NuGet.Credentials;
using NuGetPackageExplorer.Types;

namespace PackageExplorerViewModel
{
    public class CredentialDialogProvider : ICredentialProvider
    {
        private readonly Lazy<IUIServices> _uiServices;

        public CredentialDialogProvider(Lazy<IUIServices> uIServices)
        {
            _uiServices = uIServices ?? throw new ArgumentNullException(nameof(uIServices)); 
        }

        public string Id => "NPECredentialDialog";

        public async Task<CredentialResponse> GetAsync(Uri uri, IWebProxy proxy, CredentialRequestType type, string message, bool isRetry, bool nonInteractive, CancellationToken cancellationToken)
        {
            if (nonInteractive)
            {
                return new CredentialResponse(CredentialStatus.ProviderNotApplicable);
            }

            bool success = false;
            NetworkCredential credential = null;

            await _uiServices.Value.BeginInvoke(() =>
            {
                success = _uiServices.Value.OpenCredentialsDialog(uri.GetLeftPart(UriPartial.Authority), out credential);
            });

            cancellationToken.ThrowIfCancellationRequested();

            if (success)
            {
                return new CredentialResponse(credential);
            }
            return new CredentialResponse(CredentialStatus.UserCanceled);
        }
    }
}
