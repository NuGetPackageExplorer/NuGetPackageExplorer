using System;
using System.Windows;
using NuGetPe;
using PackageExplorerViewModel;

namespace PackageExplorer
{
    public partial class SignPackageDialog : StandardDialog
    {
#pragma warning disable CS8618 // Non-nullable field is uninitialized.
        public SignPackageDialog()
#pragma warning restore CS8618 // Non-nullable field is uninitialized.
        {
            InitializeComponent();

            DiagnosticsClient.TrackPageView(nameof(SignPackageDialog));
        }

        public SignPackageViewModel ViewModel => (SignPackageViewModel)DataContext;

        public string SignedPackagePath { get; private set; }

        private void OnCertificatePasswordChange(object sender, EventArgs args)
        {
            ViewModel.Password = CertificatePasswordBox.Password;
        }

        private void OnCloseButton_Click(object sender, RoutedEventArgs e)
        {
            DiagnosticsClient.TrackEvent("SignPackageDialog_CloseButtonClick");

            DialogResult = false;
        }

        private async void OnSignButton_Click(object sender, RoutedEventArgs e)
        {
            DiagnosticsClient.TrackEvent("SignPackageDialog_SignButtonClick");

            var result = await ViewModel.SignPackage();
            if (result != null)
            {
                SignedPackagePath = result;
                DialogResult = true;
            }
        }
    }
}
