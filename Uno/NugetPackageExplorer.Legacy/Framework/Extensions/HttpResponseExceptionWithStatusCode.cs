namespace System.Net.Http
{
    /// <summary>
    /// An HttpRequestException that captures the StatusCode before it is lost.
    /// </summary>
    public sealed class HttpResponseExceptionWithStatusCode(HttpStatusCode statusCode, string message) : HttpRequestException(message)
    {
        public new HttpStatusCode StatusCode { get; init; } = statusCode;
    }
}
