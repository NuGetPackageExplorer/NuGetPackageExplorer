using System;
using System.IO;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading;
using System.Threading.Tasks;
using NuGet;

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

        public async Task PushPackage(
            string apiKey, 
            Stream packageStream, 
            IPackageMetadata package, 
            bool pushAsUnlisted,
            CancellationToken cancelToken)
        {
            var handler = new HttpClientHandler();
            if (handler.SupportsRedirectConfiguration)
            {
                handler.AllowAutoRedirect = true;
            }

            var client = new System.Net.Http.HttpClient(handler);
            client.DefaultRequestHeaders.Add(ApiKeyHeader, apiKey);
            client.DefaultRequestHeaders.UserAgent.ParseAdd(_userAgent);

            var pushContent = new MultipartFormDataContent();

            var fileContent = new StreamContent(packageStream);
            fileContent.Headers.ContentType = new MediaTypeHeaderValue("application/octet-stream");
            pushContent.Add(fileContent);

            string requestUri = EnsureTrailingSlash(_source) + ServiceEndpoint;
            using (HttpResponseMessage response = await client.PutAsync(requestUri, pushContent, cancelToken))
            {
                if (!response.IsSuccessStatusCode)
                {
                    string errorMessage = "An error occurred while publishing package: " + response.ReasonPhrase;
                    throw new InvalidOperationException(errorMessage);
                }
            }

            if (pushAsUnlisted && !cancelToken.IsCancellationRequested)
            {
                string deleteRequestUri = requestUri + "/" + package.Id + "/" + package.Version;

                using (HttpResponseMessage response = await client.DeleteAsync(deleteRequestUri, cancelToken))
                {
                    if (!response.IsSuccessStatusCode)
                    {
                        string errorMessage = "An error occurred while publishing package: " + response.ReasonPhrase;
                        throw new InvalidOperationException(errorMessage);
                    }
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