using System.Windows;
using NuGetPe;

using PackageExplorerViewModel;

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
                Clipboard.SetText(viewModel.CopyValidationMessage);
            }
        }

    }
}
