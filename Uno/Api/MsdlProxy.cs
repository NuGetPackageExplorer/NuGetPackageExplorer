using System.Diagnostics;
using System.Net;

using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Logging;

namespace Api
{
    public partial class MsdlProxy(ILogger<MsdlProxy> log, IHttpClientFactory httpClientFactory)
    {
        private readonly ILogger<MsdlProxy> _log = log;
        private readonly IHttpClientFactory _httpClientFactory = httpClientFactory;


        // LoggerMessage delegate for warning about missing symbol key
        private static readonly Action<ILogger, string, Exception?> MissingSymbolKeyWarning =
            LoggerMessage.Define<string>(
                LogLevel.Warning,
                new EventId(1, nameof(MissingSymbolKey)),
                "Symbol key is missing in the request. {Details}");

        // LoggerMessage delegate for information about symbol request
        private static readonly Action<ILogger, string, Exception?> SymbolRequestInfo =
            LoggerMessage.Define<string>(
                LogLevel.Information,
                new EventId(2, nameof(SymbolRequest)),
                "Symbol request for {SymbolKey}");

        // LoggerMessage delegate for error logging
        private static readonly Action<ILogger, Exception, Exception?> ProcessingError =
            LoggerMessage.Define<Exception>(
                LogLevel.Error,
                new EventId(3, nameof(LogProcessingError)),
                "An error occurred while processing the request. {Exception}");

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


        [Function("MsdlProxy")]
        public async Task<HttpResponseData> Run(
            [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = null)] HttpRequestData req,
            CancellationToken hostCancellationToken)
        {
            Debug.Assert(req != null);
            Debug.Assert(_log != null);

            var key = req.Query["symbolkey"];
            if (string.IsNullOrEmpty(key))
            {
                MissingSymbolKey(_log, "Symbol key is required in the query string.");
                var badRequestResponse = req.CreateResponse(HttpStatusCode.BadRequest);
                await badRequestResponse.WriteStringAsync("Symbol key is required.");
                return badRequestResponse;
            }

            SymbolRequest(_log, key);

            var checksum = req.Headers.TryGetValues("SymbolChecksum", out var checksums)
                ? checksums.FirstOrDefault()
                : null;

            var uri = new Uri(new Uri("https://msdl.microsoft.com/download/symbols/"), key);

            using var pdbRequest = new HttpRequestMessage
            {
                Method = HttpMethod.Get,
                RequestUri = uri
            };

            if (checksum != null)
            {
                pdbRequest.Headers.Add("SymbolChecksum", checksum);
            }

            using var cancellationSource = CancellationTokenSource.CreateLinkedTokenSource(hostCancellationToken, req.FunctionContext.CancellationToken);

            try
            {
                using var httpClient = _httpClientFactory.CreateClient();
                using var response = await httpClient.SendAsync(pdbRequest, cancellationSource.Token).ConfigureAwait(false);

                if (!response.IsSuccessStatusCode)
                {
                    var errorResponse = req.CreateResponse(response.StatusCode);
                    var errorContent = await response.Content.ReadAsStringAsync(cancellationSource.Token);
                    await errorResponse.WriteStringAsync(errorContent, cancellationSource.Token);
                    return errorResponse;
                }

                var pdbStream = new MemoryStream();
                await response.Content.CopyToAsync(pdbStream, cancellationSource.Token).ConfigureAwait(false);
                pdbStream.Position = 0;

                var resp = req.CreateResponse(HttpStatusCode.OK);
                resp.Headers.Add("Cache-Control", "public, immutable, max-age=31536000");
                resp.Headers.Add("Content-Type", "application/octet-stream");

                await resp.WriteBytesAsync(pdbStream.ToArray(), cancellationSource.Token);

                return resp;
            }
            catch (Exception ex)
            {
                LogProcessingError(_log, ex);
                var errorResponse = req.CreateResponse(HttpStatusCode.InternalServerError);
                await errorResponse.WriteStringAsync("An internal server error occurred.");
                return errorResponse;
            }
        }
    }
}
