using System.Collections.Generic;
using System.Reflection;
using System.Windows;
using System.Windows.Documents;
using System.Windows.Input;
using NuGetPe;
using PackageExplorerViewModel;
using Clipboard = System.Windows.Forms.Clipboard;
using StringResources = PackageExplorer.Resources;

namespace PackageExplorer
{
    /// <summary>
    /// Interaction logic for AboutWindow.xaml
    /// </summary>
    public partial class AboutWindow : StandardDialog
    {

#if   STORE
        private const string Channel = "Store";
#elif NIGHTLY
        private const string Channel = "Nightly";
#elif CHOCO
        private const string Channel = "Chocolatey";
#else
        private const string Channel = "Zip";
#endif

#pragma warning disable CS8618 // Non-nullable field is uninitialized.
        public AboutWindow()
#pragma warning restore CS8618 // Non-nullable field is uninitialized.
        {
            CopyVersionCommand = new RelayCommand(CopyVersion);

            InitializeComponent();

            ProductTitle.Text = $"{StringResources.Dialog_Title} - {Channel} - ({ typeof(AboutWindow).Assembly.GetCustomAttribute<AssemblyInformationalVersionAttribute>().InformationalVersion})";

            DiagnosticsClient.TrackPageView(nameof(AboutWindow));
        }


        private void Button_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = true;
        }

        private void Hyperlink_Click(object sender, RoutedEventArgs e)
        {
            var link = (Hyperlink)sender;

            DiagnosticsClient.TrackEvent("AboutWindow_LinkClick", new Dictionary<string, string> { { "Uri", link.NavigateUri.ToString() } });

            UriHelper.OpenExternalLink(link.NavigateUri);
        }

        private void CopyVersion()
        {
            var version = $"{Channel} - ({ typeof(AboutWindow).Assembly.GetCustomAttribute<AssemblyInformationalVersionAttribute>().InformationalVersion})";

            Clipboard.SetText(version);
        }

        public ICommand CopyVersionCommand { get; }
    }
}
