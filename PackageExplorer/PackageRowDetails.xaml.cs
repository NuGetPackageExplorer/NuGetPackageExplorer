using System.Diagnostics;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using NuGet;
using PackageExplorerViewModel;

namespace PackageExplorer
{
    /// <summary>
    /// Interaction logic for PackageRowDetails.xaml
    /// </summary>
    public partial class PackageRowDetails : UserControl
    {
        private static readonly SubtracterConverter subtractConverter = new SubtracterConverter();

        public PackageRowDetails()
        {
            InitializeComponent();
        }

        public void ApplyBindings(DataGrid parent)
        {
            var gridView = (GridView)PackageGrid.View;
            Debug.Assert(parent.Columns.Count == gridView.Columns.Count);
            for (int i = 0; i < gridView.Columns.Count; i++)
            {
                var binding = new Binding("ActualWidth")
                {
                    Source = parent.Columns[i]
                };

                if (i == 0)
                {
                    // for the Id column, which is the first column, 
                    // it is slightly smaller than the parent column width
                    // due to the left margin.
                    binding.Converter = subtractConverter;
                    binding.ConverterParameter = 3;
                }

                BindingOperations.SetBinding(
                    gridView.Columns[i],
                    GridViewColumn.WidthProperty,
                    binding);
            }
        }

        public void RemoveBindings()
        {
            var gridView = (GridView)PackageGrid.View;
            for (int i = 0; i < gridView.Columns.Count; i++)
            {
                BindingOperations.ClearBinding(
                    gridView.Columns[i],
                    GridViewColumn.WidthProperty);
            }
        }

        private void PackageGrid_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            var viewModel = (PackageInfoViewModel)DataContext;
            if (viewModel != null)
            {
                viewModel.SelectedPackage = (PackageInfo)PackageGrid.SelectedItem;
            }
        }

        private void OnPackageRowDetailsLoaded(object sender, RoutedEventArgs e)
        {
            var ownerGrid = (DataGrid)Tag;

            // align the nested ListView's columns with the parent DataGrid's columns
            ApplyBindings(ownerGrid);
        }

        private void OnPackageDoubleClick(object sender, RoutedEventArgs e)
        {
            var viewModel = (PackageInfoViewModel)DataContext;
            viewModel.OpenCommand.Execute(null);
        }
    }
}
