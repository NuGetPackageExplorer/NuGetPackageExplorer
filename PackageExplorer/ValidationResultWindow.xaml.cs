using System;
using System.Globalization;
using System.Reflection;
using System.Windows;
using System.Windows.Documents;
using StringResources = PackageExplorer.Resources.Resources;

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