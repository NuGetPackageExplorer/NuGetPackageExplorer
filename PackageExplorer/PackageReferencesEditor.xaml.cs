using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using NuGet.Packaging;
using NuGetPackageExplorer.Types;
using NuGetPe;
using PackageExplorerViewModel;

namespace PackageExplorer
{
    public partial class PackageReferencesEditor : StandardDialog
    {
        private readonly ObservableCollection<EditablePackageReferenceSet> _referenceSets = new ObservableCollection<EditablePackageReferenceSet>();

#pragma warning disable CS8618 // Non-nullable field is uninitialized.
        public PackageReferencesEditor()
#pragma warning restore CS8618 // Non-nullable field is uninitialized.
        {
            InitializeComponent();

            ReferenceGroupList.DataContext = _referenceSets;
            ClearDependencyTextBox();

            DiagnosticsClient.TrackPageView(nameof(PackageReferencesEditor));
        }

        public PackageReferencesEditor(IEnumerable<PackageReferenceSet> existingReferenceSets)
            : this()
        {
            _referenceSets.AddRange(existingReferenceSets.Select(rs => new EditablePackageReferenceSet(rs)));

            if (_referenceSets.Count > 0)
            {
                ReferenceGroupList.SelectedIndex = 0;
            }
        }

        public IPackageChooser PackageChooser { get; set; }

        public ICollection<PackageReferenceSet> GetEditedReferencesSets()
        {
            return _referenceSets.Select(set => set.AsReadOnly()).ToArray();
        }

        private EditablePackageReferenceSet ActivePackageReferenceSet
        {
            get
            {
                return (EditablePackageReferenceSet)ReferenceGroupList.SelectedItem;
            }
        }

        private void OkButton_Click(object sender, RoutedEventArgs e)
        {
            if (Validation.GetHasError(TargetFrameworkBox))
            {
                return;
            }

            DiagnosticsClient.TrackEvent("PackageReferencesEditor_OkayClick");

            // before closing, try adding any pending reference
            AddNewReference();

            DialogResult = true;
        }

        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            DiagnosticsClient.TrackEvent("PackageReferencesEditor_CancelClick");
            DialogResult = false;
        }

        private void DeleteReferenceButtonClicked(object sender, RoutedEventArgs e)
        {
            DiagnosticsClient.TrackEvent("PackageReferencesEditor_DeleteReferenceClick"); 
            var hyperlink = (Hyperlink)sender;
            var reference = (string)hyperlink.DataContext;
            if (reference != null)
            {
                ActivePackageReferenceSet.References.Remove(reference);
            }
        }

        private void OnAddGroupClicked(object sender, RoutedEventArgs e)
        {
            DiagnosticsClient.TrackEvent("PackageReferencesEditor_OnAddGroupClicked");

            _referenceSets.Add(new EditablePackageReferenceSet());

            if (ReferenceGroupList.SelectedIndex == -1)
            {
                ReferenceGroupList.SelectedIndex = _referenceSets.Count - 1;
            }
        }

        private void OnRemoveGroupClicked(object sender, RoutedEventArgs e)
        {
            DiagnosticsClient.TrackEvent("PackageReferencesEditor_OnRemoveGroupClicked");

            // remember the currently selected index;
            var selectedIndex = ReferenceGroupList.SelectedIndex;

            _referenceSets.Remove((EditablePackageReferenceSet)ReferenceGroupList.SelectedItem);

            if (_referenceSets.Count > 0)
            {
                // after removal, restore the previously selected index
                selectedIndex = Math.Min(selectedIndex, _referenceSets.Count - 1);
                ReferenceGroupList.SelectedIndex = selectedIndex;
            }
        }

        private void AddReferenceButtonClicked(object sender, RoutedEventArgs e)
        {
            DiagnosticsClient.TrackEvent("PackageReferencesEditor_AddReferenceButtonClicked");

            AddNewReference();
        }

        private void AddNewReference()
        {
            var newReference = NewReferenceFile.Text.Trim();
            if (string.IsNullOrEmpty(newReference))
            {
                return;
            }

            ActivePackageReferenceSet.References.Add(newReference);

            // after reference is added, clear the textbox
            ClearDependencyTextBox();
        }

        private void ClearDependencyTextBox()
        {
            NewReferenceFile.Text = string.Empty;
        }
    }
}
