using System;
using System.Globalization;
using System.Net;
using NuGet.Resources;

namespace NuGet {
    /// <summary>
    /// This class should be used when ever you are using a link that is actually
    /// redirecting to the destination link that you want to use as the data source.
    /// A good example of that is a link that forwards like the current nuget link
    /// that is configured as a default location for nuget packages.
    /// </summary>
    public class RedirectedHttpClient : HttpClient {
        private Lazy<IHttpClient> _cachedClient = null;
        private Uri _originalUri = null;

        public RedirectedHttpClient(Uri uri)
            : base(uri) {
            _originalUri = uri;
            _cachedClient = new Lazy<IHttpClient>(EnsureClient);
        }

        public override WebResponse GetResponse() {
            return CachedClient.GetResponse();
        }

        public override Uri Uri {
            get {
                return CachedClient.Uri;
            }
        }

        private IHttpClient CachedClient {
            get {
                // Reset the Lazy IHttpClient instance if we catch an Exception so that
                // subsequent requests are able to try and create it again in case there
                // was some issue with authentication or some other request related configuration
                // If we don't do it this here then the exception is always thrown as soon as we
                // try to access _cachedClient.Value property.
                try {
                    return _cachedClient.Value;
                }
                catch (Exception) {
                    // Re-initialize the lazy object and throw the exception so that we can
                    // see what happened.
                    _cachedClient = new Lazy<IHttpClient>(EnsureClient);
                    throw;
                }
            }
        }

        private IHttpClient EnsureClient() {
            var originalClient = new HttpClient(_originalUri);
            return new HttpClient(GetResponseUri(originalClient));
        }

        private Uri GetResponseUri(HttpClient client) {
            using (WebResponse response = client.GetResponse()) {
                if (response == null) {
                    throw new InvalidOperationException(
                        String.Format(CultureInfo.CurrentCulture,
                                      NuGetResources.UnableToResolveUri,
                                      Uri));
                }

                return response.ResponseUri;
            }
        }
    }
}