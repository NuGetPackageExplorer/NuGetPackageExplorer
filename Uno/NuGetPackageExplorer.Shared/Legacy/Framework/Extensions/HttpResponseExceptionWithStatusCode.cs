using System;
using System.Collections.Generic;
using System.Text;

namespace System.Net.Http
{
    /// <summary>
    /// An HttpRequestException that captures the StatusCode before it is lost.
    /// </summary>
    public sealed class HttpResponseExceptionWithStatusCode : HttpRequestException
    {
        public HttpStatusCode StatusCode { get; init; }

        public HttpResponseExceptionWithStatusCode(HttpStatusCode statusCode, string message)
            : base(message)
            => StatusCode = statusCode;
    }
}
