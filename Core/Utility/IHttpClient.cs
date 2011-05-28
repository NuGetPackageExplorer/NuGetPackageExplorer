using System;
using System.Net;

namespace NuGet {
    public interface IHttpClient {
        string UserAgent { get; set; }
        Uri Uri { get; set; }
        IWebProxy Proxy { get; set; }

        WebRequest CreateRequest();
        void InitializeRequest(WebRequest webRequest);
    }
}