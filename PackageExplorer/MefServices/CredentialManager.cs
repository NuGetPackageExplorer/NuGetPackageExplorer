using System.ComponentModel.Composition;
using System.Net;

using PackageExplorerViewModel.Types;

namespace PackageExplorer.MefServices
{
    [Export(typeof(ICredentialManager))]
    internal sealed class CredentialManager : ICredentialManager
    {
        private readonly object _feedsLock = new object();
        private readonly List<Tuple<Uri, ICredentials>> _feeds;
        internal static readonly char[] Separator = [':'];

        public CredentialManager()
        {
            _feeds = new List<Tuple<Uri, ICredentials>>();
        }

        private bool TryAddUriCredentials(Uri feedUri, out NetworkCredential? credentials)
        {
            // Support username and password in feed URL as specified in RFC 1738
            if (!string.IsNullOrEmpty(feedUri.UserInfo))
            {
                var userInfoSplitted = feedUri.UserInfo.Split(Separator, StringSplitOptions.RemoveEmptyEntries);
                if (userInfoSplitted.Length >= 2)
                {
                    credentials = new NetworkCredential(userInfoSplitted[0], userInfoSplitted[1]);
                    Add(credentials, feedUri);
                    return true;
                }
            }
            credentials = null;
            return false;
        }

        public void Add(ICredentials credentials, Uri feedUri)
        {
            lock (_feedsLock)
            {
                _feeds.RemoveAll(x => x.Item1 == feedUri);
                _feeds.Add(new Tuple<Uri, ICredentials>(feedUri, credentials));
            }
        }

        public ICredentials GetForUri(Uri uri)
        {
            var credentials = CredentialCache.DefaultCredentials;
            lock (_feedsLock)
            {
                var matchingFeeds = _feeds.Where(x => string.Equals(uri.Scheme, x.Item1.Scheme, StringComparison.OrdinalIgnoreCase) &&
                                                      string.Equals(uri.Host, x.Item1.Host, StringComparison.OrdinalIgnoreCase) &&
                                                      uri.AbsolutePath.Contains(x.Item1.AbsolutePath, StringComparison.OrdinalIgnoreCase))
                                          .ToList();
                if (matchingFeeds.Count > 0)
                {
                    credentials = matchingFeeds.First().Item2;
                }
                else if (TryAddUriCredentials(uri, out var uriCredentials))
                {
                    credentials = uriCredentials!;
                }
            }
            return credentials;
        }
    }
}
