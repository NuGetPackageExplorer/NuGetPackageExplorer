using System;
using System.Collections.Generic;
using NuGet.Configuration;
using NuGet.Protocol;
using NuGet.Protocol.Core.Types;
using PackageExplorerViewModel.Types;

namespace PackageExplorerViewModel
{
    public static class PackageRepositoryFactory
    {
        private static IEnumerable<Lazy<INuGetResourceProvider>> _providers = Repository.Provider.GetCoreV3();

        public static SourceRepository CreateRepository(string source, ICredentialManager credentialManager)
        {
            if (source == null)
            {
                throw new ArgumentNullException("source");
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

            return Repository.CreateSource(_providers, packageSource);
        }

        public static SourceRepository CreateRepository(string source) => CreateRepository(source, null);
    }
}
