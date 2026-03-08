using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

var host = Host.CreateDefaultBuilder()
    .ConfigureFunctionsWebApplication()
    .ConfigureServices(static s =>
    {
        s.AddApplicationInsightsTelemetryWorkerService();
        s.ConfigureFunctionsApplicationInsights();
        s.AddHttpClient();

        s.Configure<LoggerFilterOptions>(static options =>
        {
            var toRemove = options.Rules.FirstOrDefault(static rule => rule.ProviderName
                == "Microsoft.Extensions.Logging.ApplicationInsights.ApplicationInsightsLoggerProvider");

            if (toRemove is not null)
            {
                options.Rules.Remove(toRemove);
            }

            options.Rules.Add(new LoggerFilterRule(
                "Microsoft.Extensions.Logging.ApplicationInsights.ApplicationInsightsLoggerProvider",
                categoryName: null,
                LogLevel.Information,
                filter: null));
        });
    })
    .Build();

host.Run();
