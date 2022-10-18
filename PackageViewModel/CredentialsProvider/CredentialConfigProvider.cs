using System;
using System.ComponentModel.Composition;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using NuGet.Configuration;
using NuGet.Credentials;

namespace PackageExplorerViewModel
{
    // Similar to https://github.com/NuGet/NuGet.Client/blob/595d508c578370c1b6ef31dbd636f434ac5b26f7/src/NuGet.Clients/NuGet.CommandLine/SettingsCredentialProvider.cs#L1
    [Export]
    public class CredentialConfigProvider : ICredentialProvider
    {
        public string Id => "NPECredentialConfig";

        public Task<CredentialResponse> GetAsync(Uri uri, IWebProxy proxy, CredentialRequestType type, string message, bool isRetry, bool nonInteractive, CancellationToken cancellationToken)
        {
            if (!isRetry)
            {
                var nugetSettings = Settings.LoadDefaultSettings(null);
                foreach (var packageSource in SettingsUtility.GetEnabledSources(nugetSettings))
                {
                    if (packageSource.Credentials != null &&
                        packageSource.Credentials.IsValid() &&
                        packageSource.TrySourceAsUri == uri)
                    {
                        return Task.FromResult(new CredentialResponse(packageSource.Credentials.ToICredentials()));
                    }
                }
            }

            return Task.FromResult(new CredentialResponse(CredentialStatus.UserCanceled));
        }
    }
}
