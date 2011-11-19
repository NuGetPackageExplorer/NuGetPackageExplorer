using System;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.IO;
using System.Net;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Json;

namespace NuGet
{
    public class GalleryServer : IGalleryServer
    {
        private const string CreatePackageService = "PackageFiles";
        private const string PackageService = "Packages";
        private const string PublishPackageService = "PublishedPackages/Publish";
        private readonly string _baseGalleryServerUrl;
        private readonly string _originalSource;
        private readonly string _userAgent;

        public GalleryServer(string source, string userAgent)
        {
            if (string.IsNullOrEmpty(source))
            {
                throw new ArgumentNullException("source");
            }

            _originalSource = source.Trim();
            _baseGalleryServerUrl = GetSafeRedirectedUri(source);
            if (_baseGalleryServerUrl.EndsWith("/", StringComparison.Ordinal))
            {
                _baseGalleryServerUrl = _baseGalleryServerUrl.Substring(0, _baseGalleryServerUrl.Length - 1);
            }
            _userAgent = userAgent;
        }

        public bool IsV1Protocol
        {
            get
            {
                return true;
            }
        }

        public string Source
        {
            get { return _originalSource; }
        }

        [SuppressMessage(
            "Microsoft.Reliability",
            "CA2000:Dispose objects before losing scope",
            Justification = "We dispose it in the Completed event handler.")]
        public void PushPackage(string apiKey, Stream packageStream, IObserver<int> progressObserver, IPackageMetadata package)
        {
            var state = new PublishState
                        {
                            PublishKey = apiKey,
                            PackageMetadata = package,
                            ProgressObserver = progressObserver
                        };

            var url =
                new Uri(String.Format(CultureInfo.InvariantCulture, 
                                      "{0}/{1}/{2}/nupkg",
                                      _baseGalleryServerUrl,
                                      CreatePackageService, 
                                      apiKey));

            var client = new WebClient();
            client.Headers[HttpRequestHeader.ContentType] = "application/octet-stream";
            client.Headers[HttpRequestHeader.UserAgent] = _userAgent;
            client.UploadProgressChanged += OnUploadProgressChanged;
            client.UploadDataCompleted += OnCreatePackageCompleted;
            client.UploadDataAsync(url, "POST", packageStream.ReadAllBytes(), state);
        }

        [SuppressMessage(
            "Microsoft.Reliability",
            "CA2000:Dispose objects before losing scope",
            Justification = "We dispose it in the Completed event handler.")]
        private void PublishPackage(PublishState state)
        {
            var url = new Uri(String.Format(CultureInfo.InvariantCulture,
                                              "{0}/{1}",
                                              _baseGalleryServerUrl,
                                              PublishPackageService));

            using (Stream requestStream = new MemoryStream())
            {
                var data = new PublishData
                           {
                               Key = state.PublishKey,
                               Id = state.PackageMetadata.Id,
                               Version = state.PackageMetadata.Version.ToString()
                           };

                var jsonSerializer = new DataContractJsonSerializer(typeof(PublishData));
                jsonSerializer.WriteObject(requestStream, data);
                requestStream.Seek(0, SeekOrigin.Begin);

                var client = new WebClient();
                client.Headers[HttpRequestHeader.ContentType] = "application/json";
                client.Headers[HttpRequestHeader.UserAgent] = _userAgent;
                client.UploadProgressChanged += OnUploadProgressChanged;
                client.UploadDataCompleted += OnPublishPackageCompleted;
                client.UploadDataAsync(url, "POST", requestStream.ReadAllBytes(), state);
            }
        }

        [SuppressMessage("Microsoft.Usage", "CA2201:DoNotRaiseReservedExceptionTypes")]
        private void OnCreatePackageCompleted(object sender, UploadDataCompletedEventArgs e)
        {
            var state = (PublishState)e.UserState;
            if (e.Error != null)
            {
                Exception error = e.Error;

                var webException = e.Error as WebException;
                if (webException != null)
                {
                    var response = (HttpWebResponse)webException.Response;
                    if (response != null)
                    {
                        if (response.StatusCode == HttpStatusCode.InternalServerError ||
                            response.StatusCode == HttpStatusCode.Unauthorized)
                        {
                            // real error message is contained inside the response body
                            using (Stream stream = response.GetResponseStream())
                            {
                                string errorMessage = stream.ReadToEnd();
                                error = new ApplicationException(errorMessage);
                            }
                        }
                        else if (response.StatusCode == HttpStatusCode.Forbidden)
                        {
                            // this is for myget.org when the api key is invalid
                            error = new ApplicationException(response.StatusDescription);
                        }
                    }
                }

                state.ProgressObserver.OnError(error);
            }
            else if (!e.Cancelled)
            {
                PublishPackage(state);
            }

            var client = (WebClient)sender;
            client.Dispose();
        }

        private void OnPublishPackageCompleted(object sender, UploadDataCompletedEventArgs e)
        {
            var state = (PublishState)e.UserState;
            if (e.Error != null)
            {
                Exception error = e.Error;

                var webException = e.Error as WebException;
                if (webException != null)
                {
                    // real error message is contained inside the response body
                    using (Stream stream = webException.Response.GetResponseStream())
                    {
                        string errorMessage = stream.ReadToEnd();
                        error = new WebException(errorMessage, webException, webException.Status, webException.Response);
                    }
                }

                state.ProgressObserver.OnError(error);
            }
            else if (!e.Cancelled)
            {
                state.ProgressObserver.OnCompleted();
            }

            var client = (WebClient)sender;
            client.Dispose();
        }

        private void OnUploadProgressChanged(object sender, UploadProgressChangedEventArgs e)
        {
            var state = (PublishState)e.UserState;
            // Hack: the UploadDataAsync only reports up to 50 percent. multiply by 2 to simulate 100. LOL
            state.ProgressObserver.OnNext(Math.Min(100, 2 * e.ProgressPercentage));
        }

        private static string GetSafeRedirectedUri(string url)
        {
            try
            {
                var uri = new Uri(url);
                var client = new RedirectedHttpClient(uri);
                return client.Uri.ToString();
            }
            catch (WebException e)
            {
                if (WebExceptionStatus.Timeout == e.Status)
                {
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
                if (e.Response != null && e.Response.ResponseUri != null)
                {
                    return e.Response.ResponseUri.ToString();
                }
                else
                {
                    return url;
                }
            }
        }

        #region Nested type: PublishState

        private class PublishState
        {
            public string PublishKey { get; set; }
            public IObserver<int> ProgressObserver { get; set; }
            public IPackageMetadata PackageMetadata { get; set; }
        }

        #endregion
    }

    [DataContract]
    public class PublishData
    {
        [DataMember(Name = "key")]
        public string Key { get; set; }

        [DataMember(Name = "id")]
        public string Id { get; set; }

        [DataMember(Name = "version")]
        public string Version { get; set; }
    }
}