namespace PackageExplorer.Platforms.WebAssembly
{
    // NuGetConfigSeeder.cs
#if __WASM__
    using System;
    using System.IO;
    using System.Linq;
    using System.Reflection;
    using System.Runtime.CompilerServices;

    internal static class NuGetConfigSeeder
    {
        private const string TargetDir = "/home/web_user/.nuget/NuGet";
        private const string TargetPath = TargetDir + "/NuGet.Config";

        [ModuleInitializer]
        internal static void Init()
        {
            Directory.CreateDirectory(TargetDir);
            if (File.Exists(TargetPath)) return;

            var asm = Assembly.GetExecutingAssembly();
            var res = asm.GetManifestResourceNames()
                         .FirstOrDefault(n => n.EndsWith(".NuGet.Config", StringComparison.OrdinalIgnoreCase));
            if (res is null) return;

            using var s = asm.GetManifestResourceStream(res)!;
            using var fs = File.Create(TargetPath);
            s.CopyTo(fs);
        }
    }
#endif

}
