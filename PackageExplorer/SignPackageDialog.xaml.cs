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

        public SignPackageViewModel ViewModel => (SignPackageViewModel)DataContext;

        public string SignedPackagePath { get; private set; }

        private void OnCertificatePasswordChange(object sender, EventArgs args)
        {
            ViewModel.Password = CertificatePasswordBox.Password;
        }

        private void OnCloseButton_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
        }

        private async void OnSignButton_Click(object sender, RoutedEventArgs e)
        {
            var result = await ViewModel.SignPackage();
            if (result != null)
            {
                SignedPackagePath = result;
                DialogResult = true;
            }
        }
    }
}
