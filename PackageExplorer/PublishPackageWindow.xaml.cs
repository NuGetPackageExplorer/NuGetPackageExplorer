using System.Windows;
using NuGetPe;
using PackageExplorerViewModel;

namespace PackageExplorer
{
    public partial class PublishPackageWindow : StandardDialog
    {
#pragma warning disable CS8618 // Non-nullable field is uninitialized.
        public PublishPackageWindow()
#pragma warning restore CS8618 // Non-nullable field is uninitialized.
        {
            InitializeComponent();

            DiagnosticsClient.TrackPageView(nameof(PublishPackageWindow));
        }

        private void CloseButton_Click(object sender, RoutedEventArgs e)
        {
            DiagnosticsClient.TrackEvent("PublishPackageWindow_CloseButtonClick");
            DialogResult = false;
        }

        private async void OnPublishButtonClick(object sender, RoutedEventArgs e)
        {
            DiagnosticsClient.TrackEvent("PublishPackageWindow_OnPublishButtonClick");
            var isValid = DialogBindingGroup.UpdateSources();
            if (isValid)
            {
                var viewModel = (PublishPackageViewModel)DataContext;
                await viewModel.PushPackage();
            }
        }
    }
}
