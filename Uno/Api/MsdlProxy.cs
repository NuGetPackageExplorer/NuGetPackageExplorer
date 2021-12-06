using System;
using System.IO;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using System.Linq;
using System.Net.Http;
using System.Threading;
using System.Diagnostics;

namespace Api
{
    public static class MsdlProxy
    {
        [FunctionName("MsdlProxy")]
        public static async Task<HttpResponseMessage> Run(
            [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = null)] HttpRequest req,
            ILogger log, CancellationToken hostCancellationToken)
        {

            Debug.Assert(req != null);
            Debug.Assert(log != null);

            var key = req.Query["symbolkey"].FirstOrDefault();
            log.LogInformation($"Symbol request for {key}");

            var checksum = req.Headers["SymbolChecksum"].FirstOrDefault();

            var uri = new Uri(new Uri("https://msdl.microsoft.com/download/symbols/"), key);

            using var request = new HttpRequestMessage
            {
                Method = HttpMethod.Get,
                RequestUri = uri
            };

            if (checksum != null)
            {
                request.Headers.Add("SymbolChecksum", checksum);
            }

            using var cancellationSource = CancellationTokenSource.CreateLinkedTokenSource(hostCancellationToken, req.HttpContext.RequestAborted);

            using var httpClient = new HttpClient();

            using var response = await httpClient.SendAsync(request, cancellationSource.Token).ConfigureAwait(false);

            response.EnsureSuccessStatusCode();

            var pdbStream = new MemoryStream();

            await response.Content.CopyToAsync(pdbStream, cancellationSource.Token).ConfigureAwait(false);
            pdbStream.Position = 0;

            var resp = new HttpResponseMessage()
            {
                Content = new StreamContent(pdbStream),
            };

            resp.Headers.Add("Cache-Control", "public, immutable, max-age=31536000");
            resp.Content.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue("application/octet-stream");

            return resp;
        }
    }
}
