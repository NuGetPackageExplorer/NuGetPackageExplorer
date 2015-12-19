using System;
using System.Net;
using System.Collections.Specialized;

namespace NuGet
{
    // For unit testing
    public interface IHttpWebResponse : IDisposable
    {
        HttpStatusCode StatusCode { get; }
        Uri ResponseUri { get; }
        string AuthenticationType { get; }
        NameValueCollection Headers { get; }
    }
}