using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using NuGet;
using NuGetPackageExplorer.Types;
using PackageExplorerViewModel;

namespace PackageExplorer
{
    public partial class PackageReferencesEditor : StandardDialog
    {
        private ObservableCollection<EditablePackageReferenceSet> _referenceSets = new ObservableCollection<EditablePackageReferenceSet>();

        public PackageReferencesEditor()
        {
            InitializeComponent();

            ReferenceGroupList.DataContext = _referenceSets;
            ClearDependencyTextBox();
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

            // before closing, try adding any pending reference
            AddNewReference();

            DialogResult = true;
        }

        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
        }

        private void DeleteReferenceButtonClicked(object sender, RoutedEventArgs e)
        {
            var hyperlink = (Hyperlink)sender;
            var reference = (string)hyperlink.DataContext;
            if (reference != null)
            {
                ActivePackageReferenceSet.References.Remove(reference);
            }
        }

        private void OnAddGroupClicked(object sender, RoutedEventArgs e)
        {
            _referenceSets.Add(new EditablePackageReferenceSet());

            if (ReferenceGroupList.SelectedIndex == -1)
            {
                ReferenceGroupList.SelectedIndex = _referenceSets.Count - 1;
            }
        }

        private void OnRemoveGroupClicked(object sender, RoutedEventArgs e)
        {
            // remember the currently selected index;
            int selectedIndex = ReferenceGroupList.SelectedIndex;

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
            AddNewReference();
        }

        private void AddNewReference()
        {
            string newReference = NewReferenceFile.Text.Trim();
            if (String.IsNullOrEmpty(newReference))
            {
                return;
            }

            ActivePackageReferenceSet.References.Add(newReference);

            // after reference is added, clear the textbox
            ClearDependencyTextBox();
        }

        private void ClearDependencyTextBox()
        {
            NewReferenceFile.Text = String.Empty;
        }
    }
}