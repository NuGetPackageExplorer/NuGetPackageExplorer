using System;
using System.IO;
using System.Net;

namespace NuGet {
    public class HttpClient : IHttpClient {
        public event EventHandler<ProgressEventArgs> ProgressAvailable = delegate { };
        public event EventHandler<WebRequestEventArgs> SendingRequest = delegate { };

        private static ICredentialProvider _credentialProvider;
        private Uri _uri;

        public HttpClient(Uri uri) {
            if (uri == null) {
                throw new ArgumentNullException("uri");
            }

            _uri = uri;
            UserAgent = HttpUtility.CreateUserAgentString("NuGet Package Explorer");
        }

        public string UserAgent {
            get;
            set;
        }

        public virtual Uri Uri {
            get {
                return _uri;
            }
            set {
                _uri = value;
            }
        }

        public string Method {
            get;
            set;
        }

        public string ContentType {
            get;
            set;
        }

        public bool AcceptCompression {
            get;
            set;
        }

        public static ICredentialProvider DefaultCredentialProvider {
            get {
                return _credentialProvider ?? NullCredentialProvider.Instance;
            }
            set {
                _credentialProvider = value;
            }
        }

        public virtual WebResponse GetResponse() {
            Func<WebRequest> webRequestFactory = () => {
                WebRequest request = WebRequest.Create(Uri);
                InitializeRequestProperties(request);
                return request;
            };

            return RequestHelper.GetResponse(webRequestFactory, 
                                             RaiseSendingRequest, 
                                             ProxyCache.Instance, 
                                             CredentialStore.Instance, 
                                             DefaultCredentialProvider);
        }

        public void InitializeRequest(WebRequest request) {
            // Setup the request properties like content type and compression
            InitializeRequestProperties(request);

            // Use credentials from the cache if any for this uri
            TrySetCredentialsAndProxy(request);

            // Give clients a chance to examine/modify the request object before the request actually goes out.
            RaiseSendingRequest(request);
        }

        private void TrySetCredentialsAndProxy(WebRequest request) {
            // Used the cached credentials and proxy we have
            request.Credentials = CredentialStore.Instance.GetCredentials(Uri);
            request.Proxy = ProxyCache.Instance.GetProxy(Uri);
        }

        private void InitializeRequestProperties(WebRequest request) {
            var httpRequest = request as HttpWebRequest;
            if (httpRequest != null) {
                httpRequest.UserAgent = UserAgent;
                httpRequest.CookieContainer = new CookieContainer();
                if (AcceptCompression) {
                    httpRequest.AutomaticDecompression = DecompressionMethods.Deflate | DecompressionMethods.GZip;
                }
            }

            if (!String.IsNullOrEmpty(ContentType)) {
                request.ContentType = ContentType;
            }

            if (!String.IsNullOrEmpty(Method)) {
                request.Method = Method;
            }
        }

        public byte[] DownloadData() {
            const int ChunkSize = 1024 * 4; // 4KB

            byte[] buffer = null;

            // we don't want to enable compression when downloading 
            using (var response = GetResponse()) {
                // Total response length
                int length = (int)response.ContentLength;
                buffer = new byte[length];

                // We read the response stream chunk by chunk (each chunk is 4KB). 
                // After reading each chunk, we report the progress based on the total number bytes read so far.
                int totalReadSoFar = 0;
                using (Stream stream = response.GetResponseStream()) {
                    while (totalReadSoFar < length) {
                        int bytesRead = stream.Read(buffer, totalReadSoFar, Math.Min(length - totalReadSoFar, ChunkSize));
                        if (bytesRead == 0) {
                            break;
                        }
                        else {
                            totalReadSoFar += bytesRead;
                            OnProgressAvailable((totalReadSoFar * 100) / length);
                        }
                    }
                }
            }

            return buffer;
        }

        private void OnProgressAvailable(int percentage) {
            ProgressAvailable(this, new ProgressEventArgs(percentage));
        }

        private void RaiseSendingRequest(WebRequest webRequest) {
            SendingRequest(this, new WebRequestEventArgs(webRequest));
        }
    }
}