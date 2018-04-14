﻿using System.Windows;
using System.Windows.Controls;
using NuGetPe;
using PackageExplorerViewModel;

namespace PackageExplorer
{
    /// <summary>
    /// Interaction logic for PackageRowDetails.xaml
    /// </summary>
    public partial class PackageDetailActionsControl : UserControl
    {
        private void PackageGrid_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            var viewModel = (PackageInfoViewModel) DataContext;
            if (viewModel != null)
            {
                viewModel.SelectedPackage = (PackageInfo) AllVersionsGrid.SelectedItem;
            }
        }

        private void OnPackageDoubleClick(object sender, RoutedEventArgs e)
        {
            var viewModel = (PackageInfoViewModel) DataContext;
            viewModel.OpenCommand.Execute(null);
        }
    }
}
