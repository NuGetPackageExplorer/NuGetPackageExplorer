using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows;
using NuGet;
using NuGetPackageExplorer.Types;

namespace PackageExplorer
{
    /// <summary>
    /// Interaction logic for PackageDependencyEditor.xaml
    /// </summary>
    public partial class PackageDependencyEditor : StandardDialog
    {
        private ObservableCollection<EditablePackageDependencySet> _dependencySets = 
            new ObservableCollection<EditablePackageDependencySet>();

        public PackageDependencyEditor()
        {
            InitializeComponent();

            DependencyGroupList.DataContext = _dependencySets;
        }

        public PackageDependencyEditor(IEnumerable<PackageDependencySet> existingDependencySets)
            : this()
        {
            _dependencySets.AddRange(existingDependencySets.Select(ds => new EditablePackageDependencySet(ds)));

            if (_dependencySets.Count > 0)
            {
                DependencyGroupList.SelectedIndex = 0;
            }
        }

        public IPackageChooser PackageChooser { get; set; }

        public ICollection<PackageDependencySet> GetEditedDependencySets()
        {
            return _dependencySets.Select(set => set.AsReadOnly()).ToArray();
        }

        private void OkButton_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = true;
        }

        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
        }

        private void RemoveDependencyButtonClicked(object sender, RoutedEventArgs e)
        {

        }

        //private void SelectDependencyButtonClicked(object sender, RoutedEventArgs e)
        //{
        //    PackageInfo selectedPackage = PackageChooser.SelectPackage(null);
        //    if (selectedPackage != null)
        //    {
        //        _newPackageDependency.Id = selectedPackage.Id;
        //        _newPackageDependency.VersionSpec = VersionUtility.ParseVersionSpec(selectedPackage.Version);
        //    }
        //}

        private void OnAddGroupClicked(object sender, RoutedEventArgs e)
        {
            _dependencySets.Add(new EditablePackageDependencySet());

            DependencyGroupList.SelectedIndex = _dependencySets.Count - 1;
        }

        private void OnRemoveGroupClicked(object sender, RoutedEventArgs e)
        {
            // remember the currently selected index;
            int selectedIndex = DependencyGroupList.SelectedIndex;

            _dependencySets.Remove((EditablePackageDependencySet)DependencyGroupList.SelectedItem);

            if (_dependencySets.Count > 0)
            {
                // after removal, restore the previously selected index
                selectedIndex = Math.Min(selectedIndex, _dependencySets.Count - 1);
                DependencyGroupList.SelectedIndex = selectedIndex;
            }
        }

        private void SelectDependencyButtonClicked(object sender, RoutedEventArgs e)
        {
            PackageInfo selectedPackage = PackageChooser.SelectPackage(null);
            if (selectedPackage != null)
            {
                _newPackageDependency.Id = selectedPackage.Id;
                _newPackageDependency.VersionSpec = VersionUtility.ParseVersionSpec(selectedPackage.Version);
            }
        }
    }
}