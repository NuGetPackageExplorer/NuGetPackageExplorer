using System;
using System.Net;

namespace PackageExplorerViewModel.Types
{
    public interface ICredentialManager
    {
        void Add(ICredentials credentials, Uri feedUri);

        ICredentials GetForUri(Uri uri);
    }
}
