using System;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using Uno.UI;
using Microsoft.UI.Xaml;

namespace PackageExplorer
{
	public class Program
	{
#if IS_WASM_SKIA
		static async Task Main(string[] args)
#else
        static int Main(string[] args)
#endif
        {
            // Ask the browser to preload these fonts to avoid relayouting content
#if IS_WASM_SKIA
			// Disabled https://github.com/unoplatform/uno-private/issues/777
			// FontFamilyHelper.PreloadAsync("Symbols", FontWeights.Normal, Windows.UI.Text.FontStretch.Normal, Windows.UI.Text.FontStyle.Normal);
#else
            // FontFamilyHelper.PreloadAsync("Symbols");
#endif

#if IS_WASM_SKIA
			var host = new Uno.UI.Runtime.Skia.WebAssembly.Browser.PlatformHost(() => new App());
			await host.Run();
#else
            Microsoft.UI.Xaml.Application.Start(_ => new App());
            return 0;
#endif
        }


        // In net8, calls to chmod for a separate library are causing issues
        // https://github.com/NuGet/NuGet.Client/blob/824a3c7d8823c3be1cf48e08e5f1993d2e8eb4ab/src/NuGet.Core/NuGet.Configuration/Utility/FileSystemUtility.cs#L53
        // Declaring the import in the main assembly help getting the p/invoke call resolved.
        [DllImport("libc.so", SetLastError = true)]
        private static extern int chmod(string pathname, int mode);
	}
}
