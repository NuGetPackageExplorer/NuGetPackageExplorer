using System.Windows;
using NuGetPe;

namespace PackageExplorer
{
    /// <summary>
    /// Interaction logic for AboutWindow.xaml
    /// </summary>
    public partial class ValidationResultWindow : StandardDialog
    {
        public ValidationResultWindow()
        {
            InitializeComponent();

            DiagnosticsClient.TrackPageView(nameof(ValidationResultWindow));
        }


        private void Button_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = true;
        }

    }
}
