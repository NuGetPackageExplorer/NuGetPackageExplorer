using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.ApplicationInsights;

var host = Host.CreateDefaultBuilder()
    .ConfigureFunctionsWebApplication()
    .ConfigureServices(static s =>
    {
        s.AddApplicationInsightsTelemetryWorkerService();
        s.ConfigureFunctionsApplicationInsights();
        s.AddHttpClient();
    })
    .ConfigureLogging(static logging => logging
        .AddFilter<ApplicationInsightsLoggerProvider>(null, LogLevel.Information))
    .Build();

host.Run();
