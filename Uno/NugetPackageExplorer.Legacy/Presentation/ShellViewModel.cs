using NupkgExplorer.Framework.MVVM;

namespace NupkgExplorer.Presentation
{
    public partial class ShellViewModel : ViewModelBase
    {
        public ViewModelBase? ActiveContent { get => GetProperty<ViewModelBase>(); set => SetProperty(value); }

        public ShellViewModel()
        {
        }
    }
}
