using System.Diagnostics;
using System.Net;
using System.Net.Http;

using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Logging;

namespace Api
{
    public partial class MsdlProxy(ILogger<MsdlProxy> log, IHttpClientFactory httpClientFactory)
    {
        private const long MaxResponseBytes = 256L * 1024L * 1024L;
        private static readonly Uri SymbolServerBaseUri = new("https://msdl.microsoft.com/download/symbols/");
        private readonly ILogger<MsdlProxy> _log = log;
        private readonly IHttpClientFactory _httpClientFactory = httpClientFactory;

        private static readonly Action<ILogger, string, Exception?> MissingSymbolKeyWarning =
            LoggerMessage.Define<string>(
                LogLevel.Warning,
                new EventId(1, nameof(MissingSymbolKey)),
                "Symbol key is missing in the request. {Details}");

        private static readonly Action<ILogger, string, Exception?> SymbolRequestInfo =
            LoggerMessage.Define<string>(
                LogLevel.Information,
                new EventId(2, nameof(SymbolRequest)),
                "Symbol request for {SymbolKey}");

        private static readonly Action<ILogger, Exception, Exception?> ProcessingError =
            LoggerMessage.Define<Exception>(
                LogLevel.Error,
                new EventId(3, nameof(LogProcessingError)),
                "An error occurred while processing the request. {Exception}");

        private static readonly Action<ILogger, HttpStatusCode, Exception?> UpstreamFailureWarning =
            LoggerMessage.Define<HttpStatusCode>(
                LogLevel.Warning,
                new EventId(4, nameof(LogUpstreamFailure)),
                "Upstream symbol server returned non-success status {StatusCode}");

        private static readonly Action<ILogger, long, Exception?> OversizedResponseWarning =
            LoggerMessage.Define<long>(
                LogLevel.Warning,
                new EventId(5, nameof(LogOversizedResponse)),
                "Rejected oversized symbol response. Size={ResponseSizeBytes}");

        private static void MissingSymbolKey(ILogger logger, string details)
        {
            MissingSymbolKeyWarning(logger, details, null);
        }

        private static void SymbolRequest(ILogger logger, string symbolKey)
        {
            SymbolRequestInfo(logger, symbolKey, null);
        }

        private static void LogProcessingError(ILogger logger, Exception exception)
        {
            ProcessingError(logger, exception, null);
        }

        private static void LogUpstreamFailure(ILogger logger, HttpStatusCode statusCode)
        {
            UpstreamFailureWarning(logger, statusCode, null);
        }

        private static void LogOversizedResponse(ILogger logger, long responseSizeBytes)
        {
            OversizedResponseWarning(logger, responseSizeBytes, null);
        }

        [Function("MsdlProxy")]
        public async Task<HttpResponseData> Run(
            [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = null)] HttpRequestData req,
            CancellationToken hostCancellationToken)
        {
            Debug.Assert(req != null);
            var key = req.Query["symbolkey"];
            if (string.IsNullOrEmpty(key))
            {
                MissingSymbolKey(_log, "Symbol key is required in the query string.");
                var badRequestResponse = req.CreateResponse(HttpStatusCode.BadRequest);
                await badRequestResponse.WriteStringAsync("Symbol key is required.", hostCancellationToken);
                return badRequestResponse;
            }

            if (!TryNormalizeSymbolKey(key, out var normalizedKey))
            {
                var badRequestResponse = req.CreateResponse(HttpStatusCode.BadRequest);
                await badRequestResponse.WriteStringAsync("Invalid symbol key.", hostCancellationToken);
                return badRequestResponse;
            }

            SymbolRequest(_log, normalizedKey);

            var checksum = req.Headers.TryGetValues("SymbolChecksum", out var checksums)
                ? checksums.FirstOrDefault()
                : null;

            using var pdbRequest = new HttpRequestMessage(HttpMethod.Get, new Uri(SymbolServerBaseUri, normalizedKey));
            if (checksum is not null)
            {
                pdbRequest.Headers.Add("SymbolChecksum", checksum);
            }

            using var cancellationSource = CancellationTokenSource.CreateLinkedTokenSource(hostCancellationToken, req.FunctionContext.CancellationToken);

            try
            {
                using var httpClient = _httpClientFactory.CreateClient();
                using var response = await httpClient.SendAsync(pdbRequest, HttpCompletionOption.ResponseHeadersRead, cancellationSource.Token).ConfigureAwait(false);

                if (!response.IsSuccessStatusCode)
                {
                    LogUpstreamFailure(_log, response.StatusCode);
                    var upstreamFailureResponse = req.CreateResponse(HttpStatusCode.BadGateway);
                    await upstreamFailureResponse.WriteStringAsync("Upstream symbol server request failed.", cancellationSource.Token);
                    return upstreamFailureResponse;
                }

                if (response.Content.Headers.ContentLength is long contentLength && contentLength > MaxResponseBytes)
                {
                    LogOversizedResponse(_log, contentLength);
                    var tooLargeResponse = req.CreateResponse(HttpStatusCode.RequestEntityTooLarge);
                    await tooLargeResponse.WriteStringAsync("Symbol response exceeded the allowed size.", cancellationSource.Token);
                    return tooLargeResponse;
                }

                await using var responseStream = await response.Content.ReadAsStreamAsync(cancellationSource.Token).ConfigureAwait(false);
                var payload = await ReadBoundedAsync(responseStream, cancellationSource.Token).ConfigureAwait(false);
                if (payload is null)
                {
                    LogOversizedResponse(_log, MaxResponseBytes);
                    var tooLargeResponse = req.CreateResponse(HttpStatusCode.RequestEntityTooLarge);
                    await tooLargeResponse.WriteStringAsync("Symbol response exceeded the allowed size.", cancellationSource.Token);
                    return tooLargeResponse;
                }

                var successResponse = req.CreateResponse(HttpStatusCode.OK);
                successResponse.Headers.Add("Cache-Control", "public, immutable, max-age=31536000");
                successResponse.Headers.Add("Content-Type", "application/octet-stream");
                await successResponse.WriteBytesAsync(payload, cancellationSource.Token);
                return successResponse;
            }
            catch (Exception ex)
            {
                LogProcessingError(_log, ex);
                var errorResponse = req.CreateResponse(HttpStatusCode.InternalServerError);
                await errorResponse.WriteStringAsync("An internal server error occurred.", cancellationSource.Token);
                return errorResponse;
            }
        }

        private static bool TryNormalizeSymbolKey(string key, out string normalizedKey)
        {
            normalizedKey = string.Empty;

            if (key.Contains('%', StringComparison.Ordinal) ||
                key.StartsWith("//", StringComparison.Ordinal) ||
                key.Contains('\\', StringComparison.Ordinal) ||
                Uri.TryCreate(key, UriKind.Absolute, out _))
            {
                return false;
            }

            var segments = key.Split('/', StringSplitOptions.None);
            if (segments.Length != 3 || segments.Any(static segment => string.IsNullOrWhiteSpace(segment)))
            {
                return false;
            }

            if (!segments[0].Equals(segments[2], StringComparison.OrdinalIgnoreCase))
            {
                return false;
            }

            if (segments.Any(static segment => segment is "." or ".." || segment.Contains("..", StringComparison.Ordinal) || segment.Any(char.IsControl)))
            {
                return false;
            }

            if (segments[1].Contains('.', StringComparison.Ordinal) || segments[1].Contains('/', StringComparison.Ordinal))
            {
                return false;
            }

            normalizedKey = string.Join("/", segments);
            return true;
        }

        private static async Task<byte[]?> ReadBoundedAsync(Stream responseStream, CancellationToken cancellationToken)
        {
            using var buffer = new MemoryStream();
            var copyBuffer = new byte[81920];
            long totalBytes = 0;

            while (true)
            {
                var bytesRead = await responseStream.ReadAsync(copyBuffer, cancellationToken).ConfigureAwait(false);
                if (bytesRead == 0)
                {
                    return buffer.ToArray();
                }

                totalBytes += bytesRead;
                if (totalBytes > MaxResponseBytes)
                {
                    return null;
                }

                await buffer.WriteAsync(copyBuffer.AsMemory(0, bytesRead), cancellationToken).ConfigureAwait(false);
            }
        }
    }
}
