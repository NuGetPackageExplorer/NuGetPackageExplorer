using System.Windows;
using PackageExplorerViewModel;

namespace PackageExplorer
{
    public partial class PublishPackageWindow : StandardDialog
    {
        public PublishPackageWindow()
        {
            InitializeComponent();
        }

        private void CloseButton_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
        }

        private void OnPublishButtonClick(object sender, RoutedEventArgs e)
        {
            bool isValid = DialogBindingGroup.UpdateSources();
            if (isValid)
            {
                var viewModel = (PublishPackageViewModel) DataContext;
#pragma warning disable CS4014 // Because this call is not awaited, execution of the current method continues before the call is completed
                viewModel.PushPackage();
#pragma warning restore CS4014 // Because this call is not awaited, execution of the current method continues before the call is completed
            }
        }
    }
}