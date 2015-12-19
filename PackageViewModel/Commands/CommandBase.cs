namespace PackageExplorerViewModel
{
    internal class CommandBase
    {
        protected CommandBase(PackageViewModel viewModel)
        {
            ViewModel = viewModel;
        }

        protected PackageViewModel ViewModel { get; private set; }
    }
}