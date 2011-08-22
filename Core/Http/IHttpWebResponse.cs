using System;
using System.Net;

namespace NuGet {
    // For unit testing
    public interface IHttpWebResponse : IDisposable {
        HttpStatusCode StatusCode { get; }
        Uri ResponseUri { get; }
        string AuthType { get; }
    }
}
