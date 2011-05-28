using System;
using System.Windows.Controls;
using NuGetPackageExplorer.Types;

namespace PackageExplorer
{
    /// <summary>
    /// Interaction logic for ContentViewerPane.xaml
    /// </summary>
    public partial class ContentViewerPane : UserControl
    {
        public ContentViewerPane()
        {
            InitializeComponent();
            PopulateLanguageBoxValues();
        }

        private void PopulateLanguageBoxValues()
        {
            LanguageBox.ItemsSource = Enum.GetValues(typeof(SourceLanguageType));
        }

        private void UserControl_DataContextChanged(object sender, System.Windows.DependencyPropertyChangedEventArgs e)
        {
            contentBox.Reparse();
        }

        private void OnLanguageBoxSelectionChanged(object sender, System.Windows.RoutedEventArgs e)
        {
            if (LanguageBox.SelectedItem != null)
            {
                contentBox.Reparse();
            }
        }
    }
}
