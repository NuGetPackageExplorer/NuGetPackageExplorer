using System;
using System.Collections.Generic;
using System.Text;

namespace NuGetPackageExplorer
{
    public static class Constants
    {
#if DEBUG
        public const string LocalTestPackagePath = "./tmp/mynuget.nupkg";
#endif

        public const string AppName = "NuGet Package Explorer";

        public const string NuGetOrgSource = "https://api.nuget.org/v3/index.json";
    }
}
