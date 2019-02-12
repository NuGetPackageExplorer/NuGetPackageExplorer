using System;
using System.ComponentModel.Composition;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using NuGet.Configuration;
using NuGet.Credentials;

namespace PackageExplorerViewModel
{
    [Export]
    public class CredentialPublishProvider : ICredentialProvider
    {
        public string Id => "NPECredentialPublish";

        public string? PersonalAccessToken { get; set; }

        public Task<CredentialResponse> GetAsync(Uri uri, IWebProxy proxy, CredentialRequestType type, string message, bool isRetry, bool nonInteractive, CancellationToken cancellationToken)
        {
            if (!string.IsNullOrEmpty(PersonalAccessToken) && !isRetry)
            {
                return Task.FromResult(new CredentialResponse(new NetworkCredential(PersonalAccessToken, string.Empty)));
            }

            return Task.FromResult(new CredentialResponse(CredentialStatus.UserCanceled));
        }
    }
}
