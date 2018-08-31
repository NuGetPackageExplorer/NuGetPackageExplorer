using System.Windows;

namespace PackageExplorer
{
    /// <summary>
    /// Interaction logic for AboutWindow.xaml
    /// </summary>
    public partial class ValidationResultWindow : StandardDialog
    {
        public ValidationResultWindow()
        {
            InitializeComponent();

        }


        private void Button_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = true;
        }

    }
}
