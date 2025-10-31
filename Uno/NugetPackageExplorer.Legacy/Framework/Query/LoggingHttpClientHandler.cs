using System.Globalization;

using Microsoft.Extensions.Logging;

namespace NupkgExplorer.Framework.Query
{
    internal class LoggingHttpClientHandler : DelegatingHandler
    {
        private static readonly Action<ILogger, string, Uri?, Exception?> RequestLog =
            LoggerMessage.Define<string, Uri?>(
                LogLevel.Debug,
                new EventId(1, nameof(LoggingHttpClientHandler)),
                "{HttpMethod} ... {RequestUri}");

        private static readonly Action<ILogger, string, int?, Uri?, Exception?> ResponseLog =
            LoggerMessage.Define<string, int?, Uri?>(
                LogLevel.Debug,
                new EventId(2, nameof(LoggingHttpClientHandler)),
                "{HttpMethod} {StatusCode} {RequestUri}");

        private readonly Lazy<ILogger> _logger = new Lazy<ILogger>(typeof(LoggingHttpClientHandler).Log);

        public LoggingHttpClientHandler(HttpMessageHandler handler) => InnerHandler = handler;

        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            var response = default(HttpResponseMessage);

            try
            {
                if (_logger.Value.IsEnabled(LogLevel.Debug))
                {
                    var method = request.Method.ToString().ToUpper(CultureInfo.InvariantCulture);
                    RequestLog(_logger.Value, method, request.RequestUri, null);
                }

                return response = await base.SendAsync(request, cancellationToken);
            }
            finally
            {
                if (_logger.Value.IsEnabled(LogLevel.Debug))
                {
                    var method = request.Method.ToString().ToUpper(CultureInfo.InvariantCulture);
                    ResponseLog(_logger.Value, method, (int?)response?.StatusCode, request.RequestUri, null);
                }
            }
        }
    }
}
