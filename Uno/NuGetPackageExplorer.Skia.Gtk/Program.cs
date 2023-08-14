using GLib;
using System;
using System.Linq;
using System.IO;

using Uno.UI.Runtime.Skia.Gtk;

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

			var host = new GtkHost(() => new App());

			host.Run();
		}
    }
}
