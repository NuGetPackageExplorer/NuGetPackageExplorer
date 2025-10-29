using Uno.UI.Hosting;

namespace PackageExplorer
{
    public class Program
    {
        public static async Task Main(string[] args)
        {
            //FeatureConfiguration.ApiInformation.NotImplementedLogLevel = Uno.Foundation.Logging.LogLevel.Debug;

            var host = UnoPlatformHostBuilder.Create()
                .App(() => new App())
                .UseWebAssembly()
                .Build();

            await host.RunAsync();
        }
    }
}
