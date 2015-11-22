using System;
using System.Collections.Concurrent;
using System.Net;

namespace NuGet
{
    internal class CredentialStore : ICredentialCache
    {
        private static readonly CredentialStore _instance = new CredentialStore();

        private readonly ConcurrentDictionary<Uri, ICredentials> _credentialCache =
            new ConcurrentDictionary<Uri, ICredentials>();

        public static CredentialStore Instance
        {
            get { return _instance; }
        }

        #region ICredentialCache Members

        public ICredentials GetCredentials(Uri uri)
        {
            Uri rootUri = UriUtility.GetRootUri(uri);

            ICredentials credentials;
            if (_credentialCache.TryGetValue(uri, out credentials) ||
                _credentialCache.TryGetValue(rootUri, out credentials))
            {
                return credentials;
            }

            return null;
        }

        public void Add(Uri uri, ICredentials credentials)
        {
            Uri rootUri = UriUtility.GetRootUri(uri);
            _credentialCache.TryAdd(uri, credentials);
            _credentialCache.AddOrUpdate(rootUri, credentials, (u, c) => credentials);
        }

        #endregion
    }
}