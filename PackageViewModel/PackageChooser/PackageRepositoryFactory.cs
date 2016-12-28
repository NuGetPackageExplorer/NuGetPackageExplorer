using System;
using NuGetPe;
using PackageExplorerViewModel.Types;

namespace PackageExplorerViewModel
{
    internal static class PackageRepositoryFactory
    {
        public static IPackageRepository CreateRepository(string source, ICredentialManager credentialManager)
        {
            if (source == null)
            {
                throw new ArgumentNullException("source");
            }

            var uri = new Uri(source);

            if (uri.IsFile)
            {
                return new LocalPackageRepository(source);
            }
            else
            {
				credentialManager.TryAddUriCredentials(uri);
                return new DataServicePackageRepository(uri, credentialManager.Get(uri));
            }
        }
    }
}