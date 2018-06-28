using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Linq;
using System.Net;
using PackageExplorerViewModel.Types;

namespace PackageExplorer.MefServices
{
    [Export(typeof(ICredentialManager))]
    internal sealed class CredentialManager : ICredentialManager
    {
        private readonly object _feedsLock = new object();
        private readonly List<Tuple<Uri, ICredentials>> _feeds;

        public CredentialManager()
        {
            _feeds = new List<Tuple<Uri, ICredentials>>();
        }

        public void TryAddUriCredentials(Uri feedUri)
        {
            // Support username and password in feed URL as specified in RFC 1738
            if (!string.IsNullOrEmpty(feedUri.UserInfo))
            {
                var userInfoSplitted = feedUri.UserInfo.Split(new[] { ':' }, StringSplitOptions.RemoveEmptyEntries);
                if (userInfoSplitted.Length >= 2)
                {
                    Add(new NetworkCredential(userInfoSplitted[0], userInfoSplitted[1]), feedUri);
                }
            }
        }

        public void Add(ICredentials credentials, Uri feedUri)
        {
            lock (_feedsLock)
            {
                _feeds.RemoveAll(x => x.Item1 == feedUri);
                _feeds.Add(new Tuple<Uri, ICredentials>(feedUri, credentials));
            }
        }

        public ICredentials Get(Uri uri)
        {
            var credentials = CredentialCache.DefaultCredentials;
            lock (_feedsLock)
            {
                var matchingFeeds = _feeds.Where(x => string.Compare(uri.Scheme, x.Item1.Scheme, StringComparison.OrdinalIgnoreCase) == 0 &&
                                                      string.Compare(uri.Host, x.Item1.Host, StringComparison.OrdinalIgnoreCase) == 0 &&
                                                      uri.AbsolutePath.Contains(x.Item1.AbsolutePath));
                if (matchingFeeds.Any())
                {
                    credentials = matchingFeeds.First().Item2;
                }
            }
            return credentials;
        }
    }
}
