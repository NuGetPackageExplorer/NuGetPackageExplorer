namespace PackageExplorerViewModel
{
    public static class Constants
    {
        public const string UserAgentClient = "NuGet Package Explorer";

        internal const string ContentForInit = "param($installPath, $toolsPath, $package)";
        internal const string ContentForInstall = "param($installPath, $toolsPath, $package, $project)";

        internal const string ContentForBuildFile = @"<?xml version=""1.0"" encoding=""utf-8""?>
<Project ToolsVersion=""4.0"" xmlns=""http://schemas.microsoft.com/developer/msbuild/2003"">

</Project>";
    }
}