using System;
using System.Collections.Generic;
using System.Linq;
using NuGet.Configuration;
using NuGet.Protocol;
using NuGet.Protocol.Core.Types;
using PackageExplorerViewModel.Types;

namespace PackageExplorerViewModel
{
    public static class PackageRepositoryFactory
    {
        public static SourceRepository CreateRepository(PackageSource packageSource, IEnumerable<Lazy<INuGetResourceProvider>> additionalProviders)
        {
            var providers = Repository.Provider.GetCoreV3();

            if (additionalProviders != null)
            {
                providers = providers.Concat(additionalProviders);
            }

            return Repository.CreateSource(providers, packageSource);
        }
        public static SourceRepository CreateRepository(PackageSource packageSource) => CreateRepository(packageSource, null);

        public static SourceRepository CreateRepository(string source, ICredentialManager credentialManager)
        {
            if (source == null)
            {
                throw new ArgumentNullException(nameof(source));
            }
            Uri uri;
            try
            {

                uri = new Uri(source);
            }
            catch (UriFormatException)
            {
                return null;
            }

            var packageSource = new PackageSource(source);

            if (!uri.IsFile)
            {
                credentialManager.TryAddUriCredentials(uri);

                var credentials = credentialManager.Get(uri);

                if (credentials != null)
                {
                    var credential = credentials.GetCredential(uri, "");
                    packageSource.Credentials = new PackageSourceCredential(source, credential.UserName, credential.Password, true);
                }
            }

            return CreateRepository(packageSource);
        }
    }
}
