using System;
using System.Diagnostics;
using System.Windows.Controls;
using System.Windows.Navigation;

namespace PackageExplorer
{
    /// <summary>
    /// Interaction logic for PackageDetailControl.xaml
    /// </summary>
    public partial class PackageDetailControl : UserControl
    {
        public PackageDetailControl()
        {
            InitializeComponent();
        }

        private void Hyperlink_OnRequestNavigate(object sender, RequestNavigateEventArgs e)
        {
            UriHelper.OpenExternalLink(e.Uri);

            e.Handled = true;
        }
    }
}
