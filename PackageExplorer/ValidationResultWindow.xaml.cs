using System.Windows;

using NuGetPe;

using PackageExplorerViewModel;

using StringResources = PackageExplorer.Resources;
using Clipboard = System.Windows.Forms.Clipboard;

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

        private void CopyButton_Click(object sender, RoutedEventArgs e)
        {
            if (DataContext is ValidationResultViewModel viewModel)
            {
                try
                {
                    Clipboard.SetText(viewModel.ValidationSummary);
                }
                catch (System.Runtime.InteropServices.ExternalException)
                {
                    MessageBox.Show(this, StringResources.Copy_Error, StringResources.Error, MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

    }
}
