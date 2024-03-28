using System;
using System.IO;
using System.Linq;

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
	}
}
