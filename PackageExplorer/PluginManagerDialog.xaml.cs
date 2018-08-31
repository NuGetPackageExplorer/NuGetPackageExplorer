using System;
using System.Windows;
using System.Windows.Input;

namespace PackageExplorer
{
    public partial class PluginManagerDialog : StandardDialog
    {
        public PluginManagerDialog()
        {
            InitializeComponent();
        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = true;
        }

        private void GoToPageCommand_Executed(object sender, ExecutedRoutedEventArgs e)
        {
            if (e.Command != NavigationCommands.GoToPage)
            {
                return;
            }

            var uri = e.Parameter as Uri;
            if (uri == null)
            {
                var url = (string)e.Parameter;
                Uri.TryCreate(url, UriKind.Absolute, out uri);
            }

            if (uri != null)
            {
                UriHelper.OpenExternalLink(uri);
            }
        }
    }
}