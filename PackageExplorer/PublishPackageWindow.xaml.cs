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

        private async void OnPublishButtonClick(object sender, RoutedEventArgs e)
        {
            var isValid = DialogBindingGroup.UpdateSources();
            if (isValid)
            {
                var viewModel = (PublishPackageViewModel)DataContext;
                await viewModel.PushPackage();
            }
        }
    }
}
