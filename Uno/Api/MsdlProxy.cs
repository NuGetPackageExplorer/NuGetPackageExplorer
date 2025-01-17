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

        public MsdlProxy(ILogger<MsdlProxy> log)
        {
            _log = log;
        }

        [Function("MsdlProxy")]
        public async Task<HttpResponseData> Run(
            [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = null)] HttpRequestData req,
            CancellationToken hostCancellationToken)
        {
            Debug.Assert(req != null);
            Debug.Assert(_log != null);

            var key = req.Query["symbolkey"];
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

            using var httpClient = new HttpClient();

            using var response = await httpClient.SendAsync(pdbRequest, cancellationSource.Token).ConfigureAwait(false);

            response.EnsureSuccessStatusCode();

            var pdbStream = new MemoryStream();

            await response.Content.CopyToAsync(pdbStream, cancellationSource.Token).ConfigureAwait(false);
            pdbStream.Position = 0;

            var resp = req.CreateResponse(HttpStatusCode.OK);
            resp.Headers.Add("Cache-Control", "public, immutable, max-age=31536000");
            resp.Headers.Add("Content-Type", "application/octet-stream");

            await resp.WriteBytesAsync(pdbStream.ToArray());

            return resp;
        }
    }
}
