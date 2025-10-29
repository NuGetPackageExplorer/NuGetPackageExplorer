using System.Runtime.InteropServices;

using Uno.UI.Hosting;

namespace PackageExplorer
{
    public class Program
    {
        public static async Task Main(string[] args)
        {
            FeatureConfiguration.ApiInformation.NotImplementedLogLevel = Uno.Foundation.Logging.LogLevel.Debug;

            var host = UnoPlatformHostBuilder.Create()
                .App(() => new App())
                .UseWebAssembly()
                .Build();

            await host.RunAsync();
        }

        // In net8, calls to chmod for a separate library are causing issues
        // https://github.com/NuGet/NuGet.Client/blob/824a3c7d8823c3be1cf48e08e5f1993d2e8eb4ab/src/NuGet.Core/NuGet.Configuration/Utility/FileSystemUtility.cs#L53
        // Declaring the import in the main assembly help getting the p/invoke call resolved.
        [DllImport("libc.so", SetLastError = true)]
        private static extern int chmod(string pathname, int mode);
    }
}
