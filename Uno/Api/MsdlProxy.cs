using System;
using System.IO;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using System.Linq;
using System.Net.Http;
using System.Threading;
using System.Diagnostics;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using System.Net;

namespace Api
{
    public class MsdlProxy
    {
        private readonly ILogger<MsdlProxy> _log;
        private readonly IHttpClientFactory _httpClientFactory;

        public MsdlProxy(ILogger<MsdlProxy> log, IHttpClientFactory httpClientFactory)
        {
            _log = log;
            _httpClientFactory = httpClientFactory;
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
                _log.LogWarning("Symbol key is missing in the request.");
                var badRequestResponse = req.CreateResponse(HttpStatusCode.BadRequest);
                await badRequestResponse.WriteStringAsync("Symbol key is required.");
                return badRequestResponse;
            }

            _log.LogInformation($"Symbol request for {key}");

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
                var httpClient = _httpClientFactory.CreateClient();
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
                _log.LogError(ex, "An error occurred while processing the request.");
                var errorResponse = req.CreateResponse(HttpStatusCode.InternalServerError);
                await errorResponse.WriteStringAsync("An internal server error occurred.");
                return errorResponse;
            }
        }
    }
}
