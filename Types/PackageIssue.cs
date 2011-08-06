
namespace NuGetPackageExplorer.Types {
    public class PackageIssue {
        public PackageIssueType Type { get; private set; }
        public string Title { get; private set; }
        public string Problem { get; private set; }
        public string Solution { get; private set; }
        public string Target { get; set; }
    }
}