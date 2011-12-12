using System;
using System.Globalization;
using System.IO;
using System.Net;
using NuGet.Resources;

namespace NuGet
{
    public class GalleryServer2 : IGalleryServer
    {
        private const string ServiceEndpoint = "/api/v2/package";
        private const string ApiKeyHeader = "X-NuGet-ApiKey";

        private readonly Lazy<Uri> _baseUri;
        private readonly string _source;
        private readonly string _userAgent;

        public GalleryServer2(string source, string userAgent)
        {
            if (String.IsNullOrEmpty(source))
            {
                throw new ArgumentException("Argument cannot be null or empty.", "source");
            }
            _source = source;
            _userAgent = userAgent;
            _baseUri = new Lazy<Uri>(ResolveBaseUrl);
        }

        public bool IsV1Protocol
        {
            get
            {
                return false;
            }
        }

        public string Source
        {
            get { return _source; }
        }

        public void PushPackage(string apiKey, Stream packageStream, IObserver<int> progressObserver, IPackageMetadata package)
        {
            HttpClient client = GetClient("", "PUT", "application/octet-stream");

            client.SendingRequest += (sender, e) =>
                {
                    var request = (HttpWebRequest)e.Request;

                    // Set the timeout to the same as the read write timeout (5 mins is the default)
                    request.Timeout = request.ReadWriteTimeout;
                    request.Headers.Add(ApiKeyHeader, apiKey);

                    var multiPartRequest = new MultipartWebRequest();
                    multiPartRequest.AddFile(packageStream, "package");

                    multiPartRequest.CreateMultipartRequest(request);
                };

            EnsureSuccessfulResponse(client, HttpStatusCode.Created, progressObserver);
        }

        private HttpClient GetClient(string path, string method, string contentType)
        {
            var baseUrl = _baseUri.Value;
            Uri requestUri = GetServiceEndpointUrl(baseUrl, path);

            var client = new HttpClient(requestUri)
            {
                ContentType = contentType,
                Method = method
            };

            if (!String.IsNullOrEmpty(_userAgent))
            {
                client.UserAgent = _userAgent;
            }

            return client;
        }

        internal static Uri GetServiceEndpointUrl(Uri baseUrl, string path)
        {
            Uri requestUri;
            if (String.IsNullOrEmpty(baseUrl.AbsolutePath.TrimStart('/')))
            {
                // If there's no host portion specified, append the url to the client.
                requestUri = new Uri(baseUrl, ServiceEndpoint + '/' + path);
            }
            else
            {
                requestUri = new Uri(baseUrl, path);
            }
            return requestUri;
        }

        private static void EnsureSuccessfulResponse(HttpClient client, HttpStatusCode expectedStatusCode, IObserver<int> progressObserver)
        {
            HttpWebResponse response = null;
            try
            {
                response = (HttpWebResponse)client.GetResponse();
                if (response != null && expectedStatusCode != response.StatusCode)
                {
                    throw new WebException(
                        String.Format(CultureInfo.CurrentCulture, NuGetResources.PackageServerError,
                                      response.StatusDescription, String.Empty));
                }
                progressObserver.OnCompleted();
            }
            catch (WebException e)
            {
                if (e.Response == null)
                {
                    throw;
                }

                response = (HttpWebResponse)e.Response;
                if (response != null && expectedStatusCode != response.StatusCode)
                {
                    Exception error = new WebException(
                        String.Format(CultureInfo.CurrentCulture, NuGetResources.PackageServerError,
                                      response.StatusDescription, e.Message), e);
                    progressObserver.OnError(error);
                }
            }
            finally
            {
                if (response != null)
                {
                    response.Close();
                    response = null;
                }
            }
        }

        private Uri ResolveBaseUrl()
        {
            Uri uri = null;

            try
            {
                var client = new RedirectedHttpClient(new Uri(Source));
                uri = client.Uri;
            }
            catch (WebException ex)
            {
                var response = (HttpWebResponse)ex.Response;
                if (response == null)
                {
                    throw;
                }

                uri = response.ResponseUri;
            }

            return EnsureTrailingSlash(uri);
        }

        private static Uri EnsureTrailingSlash(Uri uri)
        {
            string value = uri.OriginalString;
            if (!value.EndsWith("/", StringComparison.OrdinalIgnoreCase))
            {
                value += "/";
            }
            return new Uri(value);
        }
    }
}
