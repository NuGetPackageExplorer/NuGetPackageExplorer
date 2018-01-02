using System;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;

namespace PackageExplorer
{
    /// <summary>
    /// Interaction logic for RenameWindow.xaml
    /// </summary>
    public partial class RenameWindow : StandardDialog
    {
        public static readonly DependencyProperty NewNameProperty =
            DependencyProperty.Register("NewName", typeof(string), typeof(RenameWindow));

        public RenameWindow()
        {
            InitializeComponent();

            var binding = new Binding("NewName")
                          {
                              Source = this,
                              NotifyOnValidationError = true
                          };
            binding.ValidationRules.Add(NameValidationRule.Instance);

            NameBox.SetBinding(TextBox.TextProperty, binding);
        }

        public string NewName
        {
            get { return (string) GetValue(NewNameProperty); }
            set { SetValue(NewNameProperty, value); }
        }

        // Using a DependencyProperty as the backing store for NewName.  This enables animation, styling, binding, etc...

        public string Description
        {
            get { return DescriptionText.Text; }
            set { DescriptionText.Text = value ?? string.Empty; }
        }

        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
        }

        private void OkButton_Click(object sender, RoutedEventArgs e)
        {
            if (!Validation.GetHasError(NameBox))
            {
                DialogResult = true;
            }
        }

        private void DialogWithNoMinimizeAndMaximize_Loaded(object sender, RoutedEventArgs e)
        {
            NameBox.Focus();
            NameBox.SelectAll();
        }

        #region Nested type: NameValidationRule

        private class NameValidationRule : ValidationRule
        {
            public static readonly NameValidationRule Instance = new NameValidationRule();

            private NameValidationRule()
            {
            }

            public override ValidationResult Validate(object value, CultureInfo cultureInfo)
            {
                var stringValue = (string) value;
                if (stringValue != null)
                {
                    var invalidChars = Path.GetInvalidFileNameChars();
                    if (invalidChars.Any(stringValue.Contains))
                    {
                        return new ValidationResult(false, "Invalid char found in the name.");
                    }
                }

                return ValidationResult.ValidResult;
            }
        }

        #endregion
    }
}