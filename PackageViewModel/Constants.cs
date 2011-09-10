
namespace PackageExplorerViewModel {
    public static class Constants {
        public const string UserAgentClient = "NuGet Package Explorer";

        internal const string ToolsFolder = "tools";
        internal const string ContentForInit = "param($installPath, $toolsPath, $package)";
        internal const string ContentForInstall = "param($installPath, $toolsPath, $package, $project)";
    }
}
