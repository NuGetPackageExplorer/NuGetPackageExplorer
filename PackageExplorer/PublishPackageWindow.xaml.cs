using System.Windows;
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
