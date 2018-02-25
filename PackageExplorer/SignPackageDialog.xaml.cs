using PackageExplorerViewModel;
using System;
using System.Windows;

namespace PackageExplorer
{
    public partial class SignPackageDialog : StandardDialog
    {
        public SignPackageDialog()
        {
            InitializeComponent();
        }

        public string SignedPackagePath { get; private set; }

        private void OnCertificatePasswordChange(object sender, EventArgs args)
        {
            var viewModel = (SignPackageViewModel)DataContext;
            viewModel.Password = CertificatePasswordBox.Password;
        }

        private void OnCloseButton_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
        }

        private async void OnSignButton_Click(object sender, RoutedEventArgs e)
        {
            var viewModel = (SignPackageViewModel)DataContext;
            var result = await viewModel.SignPackage();
            if (result != null)
            {
                SignedPackagePath = result;
                DialogResult = true;
            }
        }
    }
}
