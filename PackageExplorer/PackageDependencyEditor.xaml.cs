using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using NuGet.Packaging;
using NuGet.Packaging.Core;
using NuGet.Versioning;
using NuGetPackageExplorer.Types;
using NuGetPe;
using PackageExplorerViewModel;

namespace PackageExplorer
{
    /// <summary>
    /// Interaction logic for PackageDependencyEditor.xaml
    /// </summary>
    public partial class PackageDependencyEditor : StandardDialog
    {
        private readonly ObservableCollection<EditablePackageDependencySet> _dependencySets = new ObservableCollection<EditablePackageDependencySet>();

        private EditablePackageDependency _newPackageDependency;

#pragma warning disable CS8618 // Non-nullable field is uninitialized.
        public PackageDependencyEditor()
#pragma warning restore CS8618 // Non-nullable field is uninitialized.
        {
            InitializeComponent();

            DependencyGroupList.DataContext = _dependencySets;
            ClearDependencyTextBox();

            DiagnosticsClient.TrackPageView(nameof(PackageDependencyEditor));
        }

        public PackageDependencyEditor(IEnumerable<PackageDependencyGroup> existingDependencySets)
            : this()
        {
            _dependencySets.AddRange(existingDependencySets.Select(ds => new EditablePackageDependencySet(ds)));

            if (_dependencySets.Count > 0)
            {
                DependencyGroupList.SelectedIndex = 0;
            }
        }

        public IPackageChooser PackageChooser { get; set; }

        public ICollection<PackageDependencyGroup> GetEditedDependencySets()
        {
            return _dependencySets.Select(set => set.AsReadOnly()).ToArray();
        }

        private EditablePackageDependencySet ActivePackageDependencySet
        {
            get
            {
                return (EditablePackageDependencySet)DependencyGroupList.SelectedItem;
            }
        }

        private void OkButton_Click(object sender, RoutedEventArgs e)
        {
            if (Validation.GetHasError(TargetFrameworkBox))
            {
                return;
            }

            DiagnosticsClient.TrackEvent("PackageDependencyEditor_OkButton");

            var canClose = string.IsNullOrEmpty(NewDependencyId.Text) || AddNewDependency();
            if (canClose)
            {
                DialogResult = true;
            }
        }

        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            DiagnosticsClient.TrackEvent("PackageDependencyEditor_CancelButton");

            DialogResult = false;
        }

        private void RemoveDependencyButtonClicked(object sender, RoutedEventArgs e)
        {
            DiagnosticsClient.TrackEvent("PackageDependencyEditor_RemoveDependencyButtonClicked");
            var hyperlink = (Hyperlink)sender;
            var selectedPackageDependency = (PackageDependency)hyperlink.DataContext;
            if (selectedPackageDependency != null)
            {
                ActivePackageDependencySet.Dependencies.Remove(selectedPackageDependency);
            }
        }

        private void SelectDependencyButtonClicked(object sender, RoutedEventArgs e)
        {
            DiagnosticsClient.TrackEvent("PackageDependencyEditor_SelectDependencyButtonClicked");

            var selectedPackage = PackageChooser.SelectPackage(null);
            if (selectedPackage != null)
            {
                _newPackageDependency.Id = selectedPackage.Id;
                _newPackageDependency.VersionSpec = VersionRange.Parse(selectedPackage.Version);
            }
        }

        private void OnAddGroupClicked(object sender, RoutedEventArgs e)
        {
            DiagnosticsClient.TrackEvent("PackageDependencyEditor_OnAddGroupClicked");

            _dependencySets.Add(new EditablePackageDependencySet());

            if (DependencyGroupList.SelectedIndex == -1)
            {
                DependencyGroupList.SelectedIndex = _dependencySets.Count - 1;
            }
        }

        private void OnRemoveGroupClicked(object sender, RoutedEventArgs e)
        {
            DiagnosticsClient.TrackEvent("PackageDependencyEditor_OnRemoveGroupClicked");

            // remember the currently selected index;
            var selectedIndex = DependencyGroupList.SelectedIndex;

            _dependencySets.Remove((EditablePackageDependencySet)DependencyGroupList.SelectedItem);

            if (_dependencySets.Count > 0)
            {
                // after removal, restore the previously selected index
                selectedIndex = Math.Min(selectedIndex, _dependencySets.Count - 1);
                DependencyGroupList.SelectedIndex = selectedIndex;
            }
        }

        private void AddDependencyButtonClicked(object sender, RoutedEventArgs e)
        {
            DiagnosticsClient.TrackEvent("PackageDependencyEditor_AddDependencyButtonClicked");

            AddNewDependency();
        }

        private bool AddNewDependency()
        {
            if (string.IsNullOrEmpty(NewDependencyId.Text) &&
                string.IsNullOrEmpty(NewDependencyVersion.Text))
            {
                return true;
            }

            if (!NewPackageDependencyGroup.UpdateSources())
            {
                return false;
            }

            ActivePackageDependencySet.Dependencies.Add(_newPackageDependency.AsReadOnly());

            // after dependency is added, clear the textbox
            ClearDependencyTextBox();

            return true;
        }

        private void ClearDependencyTextBox()
        {
            _newPackageDependency = new EditablePackageDependency(() => ActivePackageDependencySet);
            NewDependencyId.DataContext = NewDependencyVersion.DataContext = _newPackageDependency;
        }
    }
}
