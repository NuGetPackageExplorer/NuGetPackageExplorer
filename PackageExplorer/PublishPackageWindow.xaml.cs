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
                if (viewModel.UseCredentials.HasValue && viewModel.UseCredentials.Value)
                {
                    viewModel.PublishCredentialPassword = PublishCredentialPassword.Password;
                }
                await viewModel.PushPackage();
            }
        }

        private void PublishCredentialPassword_PasswordChanged(object sender, RoutedEventArgs e)
        {
            var viewModel = (PublishPackageViewModel)DataContext;
            if (viewModel.UseCredentials.HasValue && viewModel.UseCredentials.Value)
            {
                viewModel.PublishCredentialPassword = PublishCredentialPassword.Password;
            }
        }

        private void UseCredentials_Checked(object sender, RoutedEventArgs e)
        {
            var viewModel = (PublishPackageViewModel)DataContext;
            viewModel.UseCredentials = true;
            viewModel.UseApiKey = false;
            viewModel.PublishKey = null;

            PanelApiKey.Visibility = Visibility.Collapsed;
            PanelCredentials.Visibility = Visibility.Visible;
        }

        private void UseApiKey_Checked(object sender, RoutedEventArgs e)
        {
            var viewModel = (PublishPackageViewModel)DataContext;
            viewModel.UseApiKey = true;
            viewModel.UseCredentials = false;
            viewModel.PublishCredentialUsername = null;
            viewModel.PublishCredentialPassword = null;

            PanelCredentials.Visibility = Visibility.Collapsed;
            PanelApiKey.Visibility = Visibility.Visible;
        }
    }
}