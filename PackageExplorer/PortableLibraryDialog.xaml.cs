﻿using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;

namespace PackageExplorer
{
    public partial class PortableLibraryDialog : StandardDialog
    {
        public PortableLibraryDialog()
        {
            InitializeComponent();
        }

        public string GetSelectedFrameworkName()
        {
            var comboBoxes = new ComboBox[] { NetFx, SilverlightFx, WPFx };

            var builder = new StringBuilder();
            for (int i = 0; i < comboBoxes.Length; i++)
            {
                if (!comboBoxes[i].IsEnabled)
                {
                    continue;
                }

                if (builder.Length > 0)
                {
                    builder.Append('+');
                }

                builder.Append(((ComboBoxItem)comboBoxes[i].SelectedItem).Tag);
            }

            if (WindowsCheckBox.IsChecked ?? false)
            {
                builder.Append("+win8");
            }

            if (XamariniOS.IsChecked ?? false)
            {
                builder.Append("+xamarinios");
            }

            if (XamarinAndroid.IsChecked ?? false)
            {
                builder.Append("+monoandroid");
            }

            builder.Insert(0, "portable-");
            return builder.ToString();
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
            var _allCheckBoxes = new CheckBox[] { NetCheckBox, SilverlightCheckBox, WindowsCheckBox, WPCheckBox, XamarinAndroid, XamariniOS };
            var count = _allCheckBoxes.Count(p => p.IsChecked == true);
            OKButton.IsEnabled = count >= 2;
        }
    }
}