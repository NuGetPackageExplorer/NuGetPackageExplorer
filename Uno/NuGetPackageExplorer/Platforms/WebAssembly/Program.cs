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
		static int Main(string[] args)
		{
            FeatureConfiguration.ApiInformation.NotImplementedLogLevel = Uno.Foundation.Logging.LogLevel.Debug;

            Microsoft.UI.Xaml.Application.Start(_ => new App());

			return 0;
		}

		// In net8, calls to chmod for a separate library are causing issues
		// https://github.com/NuGet/NuGet.Client/blob/824a3c7d8823c3be1cf48e08e5f1993d2e8eb4ab/src/NuGet.Core/NuGet.Configuration/Utility/FileSystemUtility.cs#L53
		// Declaring the import in the main assembly help getting the p/invoke call resolved.
        [DllImport("libc.so", SetLastError = true)]
        private static extern int chmod(string pathname, int mode);
	}
}
