using System;
using System.ComponentModel.Composition;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using NuGet.Configuration;
using NuGet.Credentials;
using NuGetPackageExplorer.Types;

namespace PackageExplorerViewModel
{
    [Export]
    public class CredentialDialogProvider : ICredentialProvider
    {
        private readonly IUIServices _uiServices;

        [ImportingConstructor]
        public CredentialDialogProvider(IUIServices uiServices)
        {
            _uiServices = uiServices ?? throw new ArgumentNullException(nameof(uiServices));
        }

        public string Id => "NPECredentialDialog";

        public async Task<CredentialResponse> GetAsync(Uri uri, IWebProxy proxy, CredentialRequestType type, string message, bool isRetry, bool nonInteractive, CancellationToken cancellationToken)
        {
            if (nonInteractive)
            {
                return new CredentialResponse(CredentialStatus.ProviderNotApplicable);
            }

            var success = false;
            NetworkCredential? credential = null;

            await _uiServices.BeginInvoke(() =>
            {
                success = _uiServices.OpenCredentialsDialog(uri.GetLeftPart(UriPartial.Authority), out credential);
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
