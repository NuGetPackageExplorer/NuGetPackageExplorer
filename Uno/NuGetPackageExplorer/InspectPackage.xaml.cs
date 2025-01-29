using System.ComponentModel.Composition;
using Microsoft.UI.Xaml.Controls;
using NupkgExplorer.Presentation.Content;

namespace PackageExplorer
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class InspectPackage : Page
    {
        public InspectPackage()
        {
            this.InitializeComponent();
        }

        public InspectPackageViewModel? Model => DataContext as InspectPackageViewModel;
    }
}
