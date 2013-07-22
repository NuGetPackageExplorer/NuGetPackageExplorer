using System.Diagnostics;
using System.Windows.Controls;
using System.Windows.Data;

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
    }
}
