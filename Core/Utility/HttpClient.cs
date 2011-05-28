using System;
using System.Net;
using System.Globalization;

namespace NuGet {
    public class HttpClient : IHttpClient {
        private Uri _uri;

        public HttpClient(Uri uri) : this(uri, null) { 
        }

        public HttpClient(Uri uri, IWebProxy proxy) {
            if (uri == null) {
                throw new ArgumentNullException("uri");
            }
            _uri = uri;
            Proxy = proxy;
            UserAgent = HttpUtility.CreateUserAgentString("NuGet Package Explorer");
        }

        public virtual WebRequest CreateRequest() {
            WebRequest request = WebRequest.Create(Uri);
            InitializeRequest(request);
            return request;
        }

        public void InitializeRequest(WebRequest webRequest) {
            var httpRequest = webRequest as HttpWebRequest;
            if (httpRequest != null) {
                httpRequest.UserAgent = UserAgent;
                httpRequest.CookieContainer = new CookieContainer();
                httpRequest.UseDefaultCredentials = true;
                httpRequest.PreAuthenticate = true;
                httpRequest.AutomaticDecompression = DecompressionMethods.GZip | DecompressionMethods.Deflate;
                // default Timeout period is 100 seconds
                httpRequest.Timeout = 1000 * 200;
            }
            webRequest.Proxy = Proxy;
        }

        public string UserAgent { get; set; }
        public virtual Uri Uri {
            get { return _uri; }
            set { _uri = value; }
        }
        public IWebProxy Proxy { get; set; }
    }
}