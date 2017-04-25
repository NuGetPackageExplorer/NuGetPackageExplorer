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
            Uri uri;
            try
            {

                uri = new Uri(source);
            }
            catch (UriFormatException)
            {
                return null;
            }


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