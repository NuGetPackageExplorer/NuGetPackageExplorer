using System;
using System.Deployment.Application;
using System.Windows;
using System.Windows.Documents;

using StringResources = PackageExplorer.Resources.Resources;

namespace PackageExplorer {
    /// <summary>
    /// Interaction logic for AboutWindow.xaml
    /// </summary>
    public partial class AboutWindow : StandardDialog {
        public AboutWindow() {
            InitializeComponent();

            ProductTitle.Text = String.Format(
                "{0} {1} ({2})",
                StringResources.Dialog_Title,
                StringResources.ProductRelease,
                GetApplicationVersion().ToString());
        }

        private static Version GetApplicationVersion() {
            if (ApplicationDeployment.IsNetworkDeployed) {
                return ApplicationDeployment.CurrentDeployment.CurrentVersion;
            }
            else {
                return typeof(MainWindow).Assembly.GetName().Version;
            }
        }

        private void Button_Click(object sender, RoutedEventArgs e) {
            DialogResult = true;
        }

        private void Hyperlink_Click(object sender, RoutedEventArgs e) {
            var link = (Hyperlink)sender;
            UriHelper.OpenExternalLink(link.NavigateUri);
        }
    }
}