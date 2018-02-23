using System;
using System.Globalization;
using System.Reflection;
using System.Windows;
using System.Windows.Documents;
using StringResources = PackageExplorer.Resources;

namespace PackageExplorer
{
    /// <summary>
    /// Interaction logic for AboutWindow.xaml
    /// </summary>
    public partial class AboutWindow : StandardDialog
    {
        public AboutWindow()
        {
            InitializeComponent();
            
            ProductTitle.Text = $"{StringResources.Dialog_Title} ({ typeof(AboutWindow).Assembly.GetCustomAttribute<AssemblyInformationalVersionAttribute>().InformationalVersion})";
        }
        

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = true;
        }

        private void Hyperlink_Click(object sender, RoutedEventArgs e)
        {
            var link = (Hyperlink) sender;
            UriHelper.OpenExternalLink(link.NavigateUri);
        }
    }
}
