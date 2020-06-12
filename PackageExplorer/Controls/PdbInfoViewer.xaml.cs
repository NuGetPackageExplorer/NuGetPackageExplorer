using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace PackageExplorer.Controls
{
    /// <summary>
    /// Interaction logic for PdbInfoViewer.xaml
    /// </summary>
    public partial class PdbInfoViewer : UserControl
    {
        public PdbInfoViewer()
        {
            InitializeComponent();
        }

        // let the parent scroll viewer handle mouse wheel events instead (https://stackoverflow.com/a/3498927)
        private void DataGrid_PreviewMouseWheel(object sender, MouseWheelEventArgs e)
        {
            if (!e.Handled)
            {
                e.Handled = true;
                var eventArgs = new MouseWheelEventArgs(e.MouseDevice, e.Timestamp, e.Delta);
                eventArgs.RoutedEvent = UIElement.MouseWheelEvent;
                eventArgs.Source = sender;
                RaiseEvent(eventArgs);
            }
        }
    }
}
