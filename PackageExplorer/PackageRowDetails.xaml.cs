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

                BindingOperations.SetBinding(
                    gridView.Columns[i],
                    GridViewColumn.WidthProperty,
                    binding);
            }
        }

        public void RemoveBindings(DataGrid parent)
        {
            var gridView = (GridView)PackageGrid.View;
            Debug.Assert(parent.Columns.Count == gridView.Columns.Count);
            for (int i = 0; i < gridView.Columns.Count; i++)
            {
                BindingOperations.ClearBinding(
                    gridView.Columns[i],
                    GridViewColumn.WidthProperty);
            }
        }
    }
}
