using System;
using System.Globalization;
using System.IO;
using System.Net;
using System.Threading.Tasks;
using NuGetPe;

namespace PackageExplorerViewModel
{
    public class GalleryServer
    {
        private const string ServiceEndpoint = "api/v2/package";
        private const string ApiKeyHeader = "X-NuGet-ApiKey";

        private readonly string _source;
        private readonly string _userAgent;

        public GalleryServer(string source, string userAgent)
        {
            if (String.IsNullOrEmpty(source))
            {
                throw new ArgumentException("Argument cannot be null or empty.", "source");
            }
            _source = source;
            _userAgent = userAgent;
        }

        public string Source
        {
            get { return _source; }
        }

        public async Task PushPackage(string apiKey, string filePath, IPackageMetadata package, bool pushAsUnlisted)
        {
            string requestUri = EnsureTrailingSlash(_source) + ServiceEndpoint;
            
            HttpWebRequest httpRequest = WebRequest.CreateHttp(requestUri);
            httpRequest.Method = "PUT";
            httpRequest.AllowAutoRedirect = true;
            httpRequest.AllowWriteStreamBuffering = false;
            httpRequest.KeepAlive = false;
            httpRequest.Headers.Add(ApiKeyHeader, apiKey);
            httpRequest.UserAgent = _userAgent;

            var multipartRequest = new MultipartWebRequest();
            multipartRequest.AddFile(new FileInfo(filePath), package.ToString());

            // sending package data asynchronously
            await multipartRequest.CreateMultipartRequest(httpRequest);
            
            // waiting for response asynchronously
            await EnsureSuccessfulResponse(httpRequest, HttpStatusCode.Created);

            if (pushAsUnlisted)
            {
                await DeletePackageFromServer(apiKey, package.Id, package.Version.ToString());
            }
        }

        private Task DeletePackageFromServer(string apiKey, string packageId, string packageVersion)
        {
            string requestUri = EnsureTrailingSlash(_source) + ServiceEndpoint + "/" + packageId + "/" + packageVersion;

            HttpWebRequest httpRequest = WebRequest.CreateHttp(requestUri);
            httpRequest.Method = "DELETE";
            httpRequest.Headers.Add(ApiKeyHeader, apiKey);
            httpRequest.UserAgent = _userAgent;

            return EnsureSuccessfulResponse(httpRequest);
        }

        private static async Task EnsureSuccessfulResponse(HttpWebRequest httpRequest, HttpStatusCode? expectedStatusCode = null)
        {
            HttpWebResponse response = null;
            try
            {
                response = (HttpWebResponse)await Task.Factory.FromAsync<WebResponse>(
                     httpRequest.BeginGetResponse, httpRequest.EndGetResponse, state: null);

                if (response != null &&
                    ((expectedStatusCode.HasValue && expectedStatusCode.Value != response.StatusCode) ||

                    // If expected status code isn't provided, just look for anything 400 (Client Errors) or higher (incl. 500-series, Server Errors)
                    // 100-series is protocol changes, 200-series is success, 300-series is redirect.
                    (!expectedStatusCode.HasValue && (int)response.StatusCode >= 400)))
                {
                    throw new InvalidOperationException(
                        String.Format(CultureInfo.CurrentCulture, "Failed to process request: '{0}'.", response.StatusDescription));
                }
            }
            catch (WebException e)
            {
                if (e.Response == null)
                {
                    throw;
                }
                response = (HttpWebResponse)e.Response;
                if (expectedStatusCode != response.StatusCode)
                {
                    throw new InvalidOperationException(
                        String.Format(CultureInfo.CurrentCulture, "Failed to process request: '{0}'.", response.StatusDescription));
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

        private static string EnsureTrailingSlash(string url)
        {
            if (!url.EndsWith("/", StringComparison.OrdinalIgnoreCase))
            {
                url += "/";
            }
            return url;
        }
    }
}