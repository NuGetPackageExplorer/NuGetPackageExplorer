using NupkgExplorer.Framework.MVVM;

namespace NupkgExplorer.Presentation
{
    public class ShellViewModel : ViewModelBase
    {
        public ViewModelBase? ActiveContent { get => GetProperty<ViewModelBase>(); set => SetProperty(value); }

        public ShellViewModel()
        {
        }
    }
}
