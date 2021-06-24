using GLib;
using System;
using System.Linq;
using System.IO;

using Uno.UI.Runtime.Skia;

namespace PackageExplorer
{
	class Program
	{
		static void Main(string[] args)
		{
			ExceptionManager.UnhandledException += delegate (UnhandledExceptionArgs expArgs)
			{
				Console.WriteLine("GLIB UNHANDLED EXCEPTION" + expArgs.ExceptionObject.ToString());
				expArgs.ExitApplication = true;
			};

#if DEBUG
            ExtractTestPackage();
#endif

			var host = new GtkHost(() => new App(), args);

			host.Run();
		}

#if DEBUG // TODO: remove debug code
        private static void ExtractTestPackage()
        {
            var names = typeof(App).Assembly.GetManifestResourceNames();
            var name = names.FirstOrDefault(n => n.EndsWith(".nupkg")) ?? throw new FileNotFoundException("test package not found in manifest resources");

            Directory.CreateDirectory(Path.GetDirectoryName(NuGetPackageExplorer.Constants.LocalTestPackagePath));
            using (var file = File.OpenWrite(NuGetPackageExplorer.Constants.LocalTestPackagePath))
            {
                typeof(App).Assembly.GetManifestResourceStream(name).CopyTo(file);
            }
        }
#endif
    }
}
