using System;
using System.Globalization;
using System.IO;
using System.Net;
using System.Threading.Tasks;
using NuGet.Packaging;
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
            if (string.IsNullOrEmpty(source))
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

        public async Task PushPackage(string apiKey, string filePath, IPackageMetadata package, bool pushAsUnlisted, bool appendV2ApiToUrl)
        {
            var requestUri = CreateRequestUri(appendV2ApiToUrl);

            // Nuget Server returns Created. VSTS Returns Accepted 
            var expectedStatus = HttpStatusCode.Created | HttpStatusCode.Accepted;
            var httpRequest = CreatePushRequest(requestUri);
            ConfigureRequestAuthApi(httpRequest, apiKey);

            var multipartRequest = new MultipartWebRequest();
            multipartRequest.AddFile(new FileInfo(filePath), package.ToString());

            // sending package data asynchronously
            await multipartRequest.CreateMultipartRequest(httpRequest);

            // waiting for response asynchronously
            await EnsureSuccessfulResponse(httpRequest, expectedStatus);

            if (pushAsUnlisted)
            {
                await DeletePackageFromServer(apiKey, package.Id, package.Version.ToString(), appendV2ApiToUrl);
            }
        }
        public async Task PushPackageWithCredentials(string filePath, IPackageMetadata package, bool pushAsUnlisted, bool appendV2ApiToUrl, string username, string password)
        {
            var requestUri = CreateRequestUri(appendV2ApiToUrl);

            // Nuget Server returns Created. VSTS Returns Accepted 
            var expectedStatus = HttpStatusCode.Created | HttpStatusCode.Accepted;
            var httpRequest = CreatePushRequest(requestUri);
            ConfigureRequestAuthCredentials(httpRequest, username, password);

            var multipartRequest = new MultipartWebRequest();
            multipartRequest.AddFile(new FileInfo(filePath), package.ToString());

            // sending package data asynchronously
            await multipartRequest.CreateMultipartRequest(httpRequest);

            // waiting for response asynchronously
            await EnsureSuccessfulResponse(httpRequest, expectedStatus);

            if (pushAsUnlisted)
            {
                await DeletePackageFromServerWithCredentials(package.Id, package.Version.ToString(), appendV2ApiToUrl, username, password);
            }
        }

        private Task DeletePackageFromServer(string apiKey, string packageId, string packageVersion, bool appendV2ApiToUrl)
        {
            var requestUri = CreateRequestUri(appendV2ApiToUrl) + "/" + packageId + "/" + packageVersion;
            var httpRequest = CreateDeleteRequest(requestUri);

            ConfigureRequestAuthApi(httpRequest, apiKey);

            return EnsureSuccessfulResponse(httpRequest);
        }
        private Task DeletePackageFromServerWithCredentials(string packageId, string packageVersion, bool appendV2ApiToUrl, string username, string password)
        {
            var requestUri = CreateRequestUri(appendV2ApiToUrl) + "/" + packageId + "/" + packageVersion;
            var httpRequest = CreateDeleteRequest(requestUri);

            ConfigureRequestAuthCredentials(httpRequest, username, password);

            return EnsureSuccessfulResponse(httpRequest);
        }

        private string CreateRequestUri(bool appendV2ApiToUrl)
        {
            var source = EnsureTrailingSlash(_source);

            return appendV2ApiToUrl ? source + ServiceEndpoint : source;
        }
        private HttpWebRequest CreatePushRequest(string requestUri)
        {
            var httpRequest = WebRequest.CreateHttp(requestUri);

            httpRequest.Method = "PUT";
            httpRequest.AllowAutoRedirect = true;
            httpRequest.KeepAlive = false;
            httpRequest.UserAgent = _userAgent;
            httpRequest.Headers.Add("X-NuGet-Protocol-Version", "4.1.0");
            httpRequest.PreAuthenticate = true;
            return httpRequest;
        }
        private HttpWebRequest CreateDeleteRequest(string requestUri)
        {
            var httpRequest = WebRequest.CreateHttp(requestUri);
            httpRequest.UseDefaultCredentials = true;
            httpRequest.Method = "DELETE";
            httpRequest.UserAgent = _userAgent;
            return httpRequest;
        }
        private static void ConfigureRequestAuthCredentials(HttpWebRequest httpRequest, string username, string password)
        {
            httpRequest.Headers.Add(ApiKeyHeader, password);
            httpRequest.UseDefaultCredentials = false;
            httpRequest.Credentials = new NetworkCredential(username, password);
        }
        private static void ConfigureRequestAuthApi(HttpWebRequest httpRequest, string apiKey)
        {
            httpRequest.Headers.Add(ApiKeyHeader, apiKey);
            httpRequest.UseDefaultCredentials = true;
        }

        private static async Task EnsureSuccessfulResponse(HttpWebRequest httpRequest, HttpStatusCode? expectedStatusCode = null)
        {
            HttpWebResponse response = null;
            try
            {
                response = (HttpWebResponse)await Task.Factory.FromAsync<WebResponse>(
                     httpRequest.BeginGetResponse, httpRequest.EndGetResponse, state: null);

                if (response != null &&
                    ((expectedStatusCode.HasValue && !expectedStatusCode.Value.HasFlag(response.StatusCode)) ||

                    // If expected status code isn't provided, just look for anything 400 (Client Errors) or higher (incl. 500-series, Server Errors)
                    // 100-series is protocol changes, 200-series is success, 300-series is redirect.
                    (!expectedStatusCode.HasValue && (int)response.StatusCode >= 400)))
                {
                    throw new InvalidOperationException(
                        string.Format(CultureInfo.CurrentCulture, "Failed to process request: '{0}'.", response.StatusDescription));
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
                        string.Format(CultureInfo.CurrentCulture, "Failed to process request: '{0}'.", response.StatusDescription));
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