using System;
using System.Net;

namespace PackageExplorerViewModel.Types
{
    public interface ICredentialManager
    {
        void TryAddUriCredentials(Uri feedUri);

        void Add(ICredentials credentials, Uri feedUri);

        ICredentials Get(Uri uri);
    }
}
