using NuGet.Frameworks;
using PackageExplorer.ViewModels;
using PackageExplorerViewModel;
using PropertyChanged;

namespace PackageExplorer
{
    [ImplementPropertyChanged]
    public partial class PortableLibraryDialog : StandardDialog
    {
        public PortableLibraryViewModel ViewModel { get; } = new PortableLibraryViewModel();

        public PortableLibraryDialog()
        {
            InitializeComponent();

            DataContext = ViewModel;

            ViewModel.SaveCommand = new RelayCommand(
                () => { DialogResult = true; },
                () => { return ViewModel.IsValidTargetedFrameworkPath(); });

            ViewModel.CancelCommand = new RelayCommand(() => { DialogResult = false; });
        }

        public string GetSelectedFrameworkName()
        {
            return ViewModel.Model.PortableFrameworks.AsTargetedPlatformPath();
        }
    }
}