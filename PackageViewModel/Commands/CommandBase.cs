
namespace PackageExplorerViewModel {
    internal class CommandBase {

        protected CommandBase(PackageViewModel viewModel) {
            this.ViewModel = viewModel;
        }

        protected PackageViewModel ViewModel { get; private set; }
    }
}
