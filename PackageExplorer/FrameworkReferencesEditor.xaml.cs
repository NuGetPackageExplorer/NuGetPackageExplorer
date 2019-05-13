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
    public partial class FrameworkReferencesEditor : StandardDialog
    {
        private readonly ObservableCollection<EditableFrameworkReferenceGroup> _referenceSets = new ObservableCollection<EditableFrameworkReferenceGroup>();

#pragma warning disable CS8618 // Non-nullable field is uninitialized.
        public FrameworkReferencesEditor()
#pragma warning restore CS8618 // Non-nullable field is uninitialized.
        {
            InitializeComponent();

            ReferenceGroupList.DataContext = _referenceSets;
            ClearDependencyTextBox();

            DiagnosticsClient.TrackPageView(nameof(FrameworkReferencesEditor));
        }

        public FrameworkReferencesEditor(IEnumerable<FrameworkReferenceGroup> existingReferenceSets)
            : this()
        {
            _referenceSets.AddRange(existingReferenceSets.Select(rs => new EditableFrameworkReferenceGroup(rs)));

            if (_referenceSets.Count > 0)
            {
                ReferenceGroupList.SelectedIndex = 0;
            }
        }

        public IPackageChooser PackageChooser { get; set; }

        public ICollection<FrameworkReferenceGroup> GetEditedReferences()
        {
            return _referenceSets.Where(set => set.TargetFramework != null).Select(set => set.AsReadOnly()).ToArray();
        }

        private EditableFrameworkReferenceGroup ActivePackageReferenceSet
        {
            get
            {
                return (EditableFrameworkReferenceGroup)ReferenceGroupList.SelectedItem;
            }
        }

        private void OkButton_Click(object sender, RoutedEventArgs e)
        {
            if (Validation.GetHasError(TargetFrameworkBox))
            {
                return;
            }

            DiagnosticsClient.TrackEvent("FrameworkReferencesEditor_OkayClick");

            // before closing, try adding any pending reference
            AddNewReference();

            DialogResult = true;
        }

        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            DiagnosticsClient.TrackEvent("FrameworkReferencesEditor_CancelClick");
            DialogResult = false;
        }

        private void DeleteReferenceButtonClicked(object sender, RoutedEventArgs e)
        {
            DiagnosticsClient.TrackEvent("FrameworkReferencesEditor_DeleteReferenceClick"); 
            var hyperlink = (Hyperlink)sender;
            var reference = (string)hyperlink.DataContext;
            if (reference != null)
            {
                ActivePackageReferenceSet.FrameworkReferences.Remove(reference);
            }
        }

        private void OnAddGroupClicked(object sender, RoutedEventArgs e)
        {
            DiagnosticsClient.TrackEvent("FrameworkReferencesEditor_OnAddGroupClicked");

            _referenceSets.Add(new EditableFrameworkReferenceGroup());

            if (ReferenceGroupList.SelectedIndex == -1)
            {
                ReferenceGroupList.SelectedIndex = _referenceSets.Count - 1;
            }
        }

        private void OnRemoveGroupClicked(object sender, RoutedEventArgs e)
        {
            DiagnosticsClient.TrackEvent("FrameworkReferencesEditor_OnRemoveGroupClicked");

            // remember the currently selected index;
            var selectedIndex = ReferenceGroupList.SelectedIndex;

            _referenceSets.Remove((EditableFrameworkReferenceGroup)ReferenceGroupList.SelectedItem);

            if (_referenceSets.Count > 0)
            {
                // after removal, restore the previously selected index
                selectedIndex = Math.Min(selectedIndex, _referenceSets.Count - 1);
                ReferenceGroupList.SelectedIndex = selectedIndex;
            }
        }

        private void AddReferenceButtonClicked(object sender, RoutedEventArgs e)
        {
            DiagnosticsClient.TrackEvent("FrameworkReferencesEditor_AddReferenceButtonClicked");

            AddNewReference();
        }

        private void AddNewReference()
        {
            var newReference = NewReferenceFile.Text.Trim();
            if (string.IsNullOrEmpty(newReference))
            {
                return;
            }

            ActivePackageReferenceSet.FrameworkReferences.Add(newReference);

            // after reference is added, clear the textbox
            ClearDependencyTextBox();
        }

        private void ClearDependencyTextBox()
        {
            NewReferenceFile.Text = string.Empty;
        }
    }
}
