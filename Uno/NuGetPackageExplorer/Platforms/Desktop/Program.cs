﻿using Uno.UI.Hosting;
using Uno.UI.Runtime.Skia;

namespace PackageExplorer;

public class Program
{
    [STAThread]
    public static void Main(string[] args)
    {
        App.InitializeLogging();

        var host = UnoPlatformHostBuilder.Create()
            .App(() => new App())
            .UseX11()
            .UseLinuxFrameBuffer()
            .UseMacOS()
            .UseWindows()
            .Build();

        host.Run();
    }
}
