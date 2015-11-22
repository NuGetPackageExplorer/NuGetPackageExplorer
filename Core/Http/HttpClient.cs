using System;
using System.IO;
using System.Net;

namespace NuGet
{
    public class HttpClient : IHttpClient
    {
        private static ICredentialProvider _credentialProvider;
        private Uri _uri;

        public HttpClient(Uri uri)
        {
            if (uri == null)
            {
                throw new ArgumentNullException("uri");
            }

            _uri = uri;
            UserAgent = HttpUtility.CreateUserAgentString("NuGet Package Explorer");
        }

        public string Method { get; set; }

        public string ContentType { get; set; }

        public static ICredentialProvider DefaultCredentialProvider
        {
            get { return _credentialProvider ?? NullCredentialProvider.Instance; }
            set { _credentialProvider = value; }
        }

        #region IHttpClient Members

        public event EventHandler<ProgressEventArgs> ProgressAvailable = delegate { };
        public event EventHandler<WebRequestEventArgs> SendingRequest = delegate { };
        public string UserAgent { get; set; }

        public virtual Uri Uri
        {
            get { return _uri; }
            set { _uri = value; }
        }

        public bool AcceptCompression { get; set; }

        public virtual WebResponse GetResponse()
        {
            Func<WebRequest> webRequestFactory = () =>
                                                 {
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

        public void InitializeRequest(WebRequest request)
        {
            // Setup the request properties like content type and compression
            InitializeRequestProperties(request);

            // Use credentials from the cache if any for this uri
            TrySetCredentialsAndProxy(request);

            // Give clients a chance to examine/modify the request object before the request actually goes out.
            RaiseSendingRequest(request);
        }

        public byte[] DownloadData()
        {
            const int chunkSize = 1024*4; // 4KB

            byte[] buffer;
            using (WebResponse response = GetResponse())
            {
                // Total response length
                var length = (int) response.ContentLength;
                using (Stream stream = response.GetResponseStream())
                {
                    // in some circumstances, the Content-Length response header is missing, resulting in
                    // the ContentLength = -1. In which case, we copy the whole stream and do not report progress.
                    if (length < 0)
                    {
                        using (var memoryStream = new MemoryStream())
                        {
                            stream.CopyTo(memoryStream, chunkSize);
                            buffer = memoryStream.ToArray();
                        }

                        // reporting fake progress as 100%
                        OnProgressAvailable(100);
                    }
                    else
                    {
                        // We read the response stream chunk by chunk (each chunk is 4KB). 
                        // After reading each chunk, we report the progress based on the total number bytes read so far.
                        int totalReadSoFar = 0;
                        buffer = new byte[length];
                        while (totalReadSoFar < length)
                        {
                            int bytesRead = stream.Read(buffer, totalReadSoFar,
                                                        Math.Min(length - totalReadSoFar, chunkSize));
                            if (bytesRead == 0)
                            {
                                break;
                            }
                            else
                            {
                                totalReadSoFar += bytesRead;
                                OnProgressAvailable((totalReadSoFar*100)/length);
                            }
                        }
                    }
                }
            }

            return buffer;
        }

        #endregion

        private void TrySetCredentialsAndProxy(WebRequest request)
        {
            // Used the cached credentials and proxy we have
            request.Credentials = CredentialStore.Instance.GetCredentials(Uri);
            request.Proxy = ProxyCache.Instance.GetProxy(Uri);
            STSAuthHelper.PrepareSTSRequest(request);
        }

        private void InitializeRequestProperties(WebRequest request)
        {
            var httpRequest = request as HttpWebRequest;
            if (httpRequest != null)
            {
                httpRequest.UserAgent = UserAgent ?? HttpUtility.CreateUserAgentString("NuGet Package Explorer");
                httpRequest.CookieContainer = new CookieContainer();
                if (AcceptCompression)
                {
                    httpRequest.AutomaticDecompression = DecompressionMethods.Deflate | DecompressionMethods.GZip;
                }
            }

            if (!String.IsNullOrEmpty(ContentType))
            {
                request.ContentType = ContentType;
            }

            if (!String.IsNullOrEmpty(Method))
            {
                request.Method = Method;
            }
        }

        private void OnProgressAvailable(int percentage)
        {
            ProgressAvailable(this, new ProgressEventArgs(percentage));
        }

        private void RaiseSendingRequest(WebRequest webRequest)
        {
            SendingRequest(this, new WebRequestEventArgs(webRequest));
        }
    }
}