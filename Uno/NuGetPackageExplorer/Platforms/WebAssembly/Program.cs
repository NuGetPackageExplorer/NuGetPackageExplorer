using System.Runtime.InteropServices;

using SkiaSharp;

using Uno.UI.Hosting;

namespace PackageExplorer
{
    public class Program
    {
        public static async Task Main(string[] args)
        {
            //FeatureConfiguration.ApiInformation.NotImplementedLogLevel = Uno.Foundation.Logging.LogLevel.Debug;

            var host = UnoPlatformHostBuilder.Create()
                .App(static () => new App())
                .UseWebAssembly()
                .Build();

            await host.RunAsync();
        }

        static async void TestSkia()
        {
            var imageBytes = Marshal.AllocHGlobal(100);
            SKData.Create(imageBytes, 100, (_, _) =>
            {
                Console.WriteLine("SKData DISPOSED!");
                Marshal.FreeHGlobal(imageBytes);
            });
            while (true)
            {
                await Task.Delay(25);
                GC.Collect();
                GC.WaitForPendingFinalizers();
                GC.Collect();
                GC.WaitForPendingFinalizers();
            }
        }

        static Program()
        {
            TestSkia();
        }
    }
}
