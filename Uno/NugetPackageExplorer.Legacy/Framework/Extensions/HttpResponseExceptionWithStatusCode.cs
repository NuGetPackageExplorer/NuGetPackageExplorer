namespace System.Net.Http
{
    /// <summary>
    /// An HttpRequestException that captures the StatusCode before it is lost.
    /// </summary>
    public sealed class HttpResponseExceptionWithStatusCode : HttpRequestException
    {
        public HttpResponseExceptionWithStatusCode()
        {
        }

        public HttpResponseExceptionWithStatusCode(string message)
            : base(message)
        {
        }

        public HttpResponseExceptionWithStatusCode(string message, Exception innerException)
            : base(message, innerException)
        {
        }

        public HttpResponseExceptionWithStatusCode(HttpStatusCode statusCode, string message)
            : base(message)
        {
            StatusCode = statusCode;
        }

        public new HttpStatusCode? StatusCode { get; init; }
    }
}
