
namespace PackageExplorerViewModel.Types {
    public class PackageProblem {
        public PackageProblemType Type { get; private set; }
        public string Title { get; private set; }
        public string Problem { get; private set; }
        public string Solution { get; private set; }
    }

    public enum PackageProblemType {
        Warning,
        Error
    }
}