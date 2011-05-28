using System;
using System.Globalization;
using System.IO;
using System.Net;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Json;

namespace NuGet {
    public class GalleryServer {
        private const string CreatePackageService = "PackageFiles";
        private const string PackageService = "Packages";
        private const string PublishPackageService = "PublishedPackages/Publish";
        private readonly string _userAgent;
        private readonly string _baseGalleryServerUrl;
        private readonly IWebProxy _internalProxy;

        public GalleryServer(string userAgent, string galleryServerSource, IProxyService proxyService) {
            if (string.IsNullOrEmpty(galleryServerSource)) {
                throw new ArgumentNullException("galleryServerSource");
            }
            if (null == proxyService) {
                throw new ArgumentNullException("proxyService");
            }
            _internalProxy = proxyService.GetProxy(new Uri(galleryServerSource));
            _baseGalleryServerUrl = GetSafeRedirectedUri(galleryServerSource);
            _userAgent = userAgent;
        }

        [System.Diagnostics.CodeAnalysis.SuppressMessage(
            "Microsoft.Reliability",
            "CA2000:Dispose objects before losing scope",
            Justification = "We dispose it in the Completed event handler.")]
        public void CreatePackage(string apiKey, Stream packageStream, IObserver<int> progressObserver, IPackageMetadata metadata = null) {
            var state = new PublishState {
                PublishKey = apiKey,
                PackageMetadata = metadata,
                ProgressObserver = progressObserver
            };

            var url = new Uri(String.Format(CultureInfo.InvariantCulture, "{0}/{1}/{2}/nupkg", _baseGalleryServerUrl, CreatePackageService, apiKey));

            WebClient client = new WebClient();
            client.Proxy = _internalProxy;
            client.Headers[HttpRequestHeader.ContentType] = "application/octet-stream";
            client.Headers[HttpRequestHeader.UserAgent] = _userAgent;
            client.UploadProgressChanged += OnUploadProgressChanged;
            client.UploadDataCompleted += OnCreatePackageCompleted;
            client.UploadDataAsync(url, "POST", packageStream.ReadAllBytes(), state);
        }

        [System.Diagnostics.CodeAnalysis.SuppressMessage(
            "Microsoft.Reliability",
            "CA2000:Dispose objects before losing scope",
            Justification = "We dispose it in the Completed event handler.")]
        private void PublishPackage(PublishState state) {
            var url = new Uri(String.Format(CultureInfo.InvariantCulture, "{0}/{1}", _baseGalleryServerUrl, PublishPackageService));

            using (Stream requestStream = new MemoryStream()) {
                var data = new PublishData {
                    Key = state.PublishKey,
                    Id = state.PackageMetadata.Id,
                    Version = state.PackageMetadata.Version.ToString()
                };

                var jsonSerializer = new DataContractJsonSerializer(typeof(PublishData));
                jsonSerializer.WriteObject(requestStream, data);
                requestStream.Seek(0, SeekOrigin.Begin);

                WebClient client = new WebClient();
                client.Proxy = _internalProxy;
                client.Headers[HttpRequestHeader.ContentType] = "application/json";
                client.Headers[HttpRequestHeader.UserAgent] = _userAgent;
                client.UploadProgressChanged += OnUploadProgressChanged;
                client.UploadDataCompleted += OnPublishPackageCompleted;
                client.UploadDataAsync(url, "POST", requestStream.ReadAllBytes(), state);
            }
        }

        private void OnCreatePackageCompleted(object sender, UploadDataCompletedEventArgs e) {
            var state = (PublishState)e.UserState;
            if (e.Error != null) {
                Exception error = e.Error;

                WebException webException = e.Error as WebException;
                if (webException != null) {
                    var response = (HttpWebResponse)webException.Response;
                    if (response.StatusCode == HttpStatusCode.InternalServerError) {
                        // real error message is contained inside the response body
                        using (Stream stream = response.GetResponseStream()) {
                            string errorMessage = stream.ReadToEnd();
                            error = new WebException(errorMessage, webException, webException.Status,
                                                     webException.Response);
                        }
                    }
                }

                state.ProgressObserver.OnError(error);
            }
            else if (!e.Cancelled) {
                if (state.PackageMetadata != null) {
                    PublishPackage(state);
                }
                else {
                    state.ProgressObserver.OnCompleted();
                }
            }

            var client = (WebClient)sender;
            client.Dispose();
        }

        private void OnPublishPackageCompleted(object sender, UploadDataCompletedEventArgs e) {
            var state = (PublishState)e.UserState;
            if (e.Error != null) {
                Exception error = e.Error;

                WebException webException = e.Error as WebException;
                if (webException != null) {
                    // real error message is contained inside the response body
                    using (Stream stream = webException.Response.GetResponseStream()) {
                        string errorMessage = stream.ReadToEnd();
                        error = new WebException(errorMessage, webException, webException.Status, webException.Response);
                    }
                }

                state.ProgressObserver.OnError(error);
            }
            else if (!e.Cancelled) {
                state.ProgressObserver.OnCompleted();
            }

            var client = (WebClient)sender;
            client.Dispose();
        }

        private void OnUploadProgressChanged(object sender, UploadProgressChangedEventArgs e) {
            var state = (PublishState)e.UserState;
            // Hack: the UploadDataAsync only reports up to 50 percent. multiply by 2 to simulate 100. LOL
            state.ProgressObserver.OnNext(Math.Min(100, 2 * e.ProgressPercentage));
        }

        private string GetSafeRedirectedUri(string url) {
            try {
                Uri uri = new Uri(url);
                IWebProxy proxy = _internalProxy;
                RedirectedHttpClient client = new RedirectedHttpClient(uri, proxy);
                return client.Uri.ToString();
            }
            catch (WebException e) {
                if (WebExceptionStatus.Timeout == e.Status) {
                    // rethrow the error because if ran into a timeout issue then
                    // we don't want the code to continue as there is not going to be any good
                    // result if we can't return a valid url to the caller.
                    throw;
                }
                // we are assuming here that we just got a 403 - Forbidden: Access is denied error
                // because we are navigating to the publish url of the Gallery Server so we simply
                // catch the error and return the response url of the response that can be used for publishing
                // the reason why we get this error is because this is a POST action and IIS gives us this error
                // because it thinks that we are trying to navigate to a page.
                return e.Response.ResponseUri.ToString();
            }
        }

        private class PublishState {
            public string PublishKey { get; set; }
            public IObserver<int> ProgressObserver { get; set; }
            public IPackageMetadata PackageMetadata { get; set; }
        }
    }

    [DataContract]
    public class PublishData {
        [DataMember(Name = "key")]
        public string Key { get; set; }

        [DataMember(Name = "id")]
        public string Id { get; set; }

        [DataMember(Name = "version")]
        public string Version { get; set; }
    }
}