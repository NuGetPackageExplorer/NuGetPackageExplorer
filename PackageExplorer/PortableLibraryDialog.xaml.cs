using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using NuGet.Frameworks;
using PackageExplorer.ViewModels;
using PropertyChanged;

namespace PackageExplorer
{
    [ImplementPropertyChanged]
    public partial class PortableLibraryDialog : StandardDialog
    {
        public PortableLibraryViewModel ViewModel { get; } = new PortableLibraryViewModel();

        public PortableLibraryDialog()
        {
            InitializeComponent();

            DataContext = ViewModel.Model;
        }

        public string GetSelectedFrameworkName()
        {
            return ViewModel.Model.PortableFrameworks.AsTargetedPlatform();
        }

        private void OkButton_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = true;
        }

        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
        }

        private void EvaluateButtonEnabledState(object sender, RoutedEventArgs e)
        {
            //var _allCheckBoxes = new CheckBox[] { NetCheckBox, SilverlightCheckBox, WindowsCheckBox, WPCheckBox, XamarinAndroid, XamariniOS };
            //var count = _allCheckBoxes.Count(p => p.IsChecked == true);
            OKButton.IsEnabled = ViewModel.Model.PortableFrameworks.AsTargetedPlatform().Length > 2; // count >= 2;
        }
    }
}