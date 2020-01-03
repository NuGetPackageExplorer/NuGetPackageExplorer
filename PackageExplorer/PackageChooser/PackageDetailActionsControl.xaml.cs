using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;
using NuGetPe;
using PackageExplorerViewModel;

namespace PackageExplorer
{
    /// <summary>
    /// Interaction logic for PackageDetailActionsControl.xaml
    /// </summary>
    public partial class PackageDetailActionsControl : UserControl
    {
#pragma warning disable CS8618 // Non-nullable field is uninitialized.
        public PackageDetailActionsControl()
#pragma warning restore CS8618 // Non-nullable field is uninitialized.
        {
            InitializeComponent();
        }

        private void PackageGrid_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            var viewModel = (PackageInfoViewModel)DataContext;
            if (viewModel != null)
            {
                viewModel.SelectedPackage = (PackageInfo)AllVersionsGrid.SelectedItem;
            }
        }

        private void OnPackageDoubleClick(object sender, RoutedEventArgs e)
        {
            var viewModel = (PackageInfoViewModel)DataContext;
            viewModel.OpenCommand.Execute(null);
        }

        protected override void OnPropertyChanged(DependencyPropertyChangedEventArgs e)
        {
            base.OnPropertyChanged(e);

            if (e.Property == DataContextProperty)
            {
                if (e.NewValue is PackageInfoViewModel newViewModel)
                {
                    newViewModel.PropertyChanged += PackageInfoViewModel_PropertyChanged;
                }
                if (e.OldValue is PackageInfoViewModel oldViewModel)
                {
                    oldViewModel.PropertyChanged -= PackageInfoViewModel_PropertyChanged;
                }
            }
        }

        private void PackageInfoViewModel_PropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(PackageInfoViewModel.HasFinishedLoading))
            {
                foreach (var column in PackageGridView.Columns)
                {
                    if (double.IsNaN(column.Width))
                    {
                        column.Width = 0;
                        column.Width = double.NaN;
                    }
                }
            }
        }
    }
}
