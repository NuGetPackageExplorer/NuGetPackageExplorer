using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using NuGet;
using NuGetPackageExplorer.Types;
using PackageExplorerViewModel;

namespace PackageExplorer
{
    /// <summary>
    /// Interaction logic for PackageMetadataEditor.xaml
    /// </summary>
    public partial class PackageMetadataEditor : UserControl, IPackageEditorService
    {
        private ObservableCollection<string> _filteredAssemblyReferences;
        private ObservableCollection<FrameworkAssemblyReference> _frameworkAssemblies;
        private EditableFrameworkAssemblyReference _newFrameworkAssembly;
        private ICollection<PackageDependencySet> _dependencySets;

        public PackageMetadataEditor()
        {
            InitializeComponent();
            PopulateLanguagesForLanguageBox();
            PopulateFrameworkAssemblyNames();
        }

        public IUIServices UIServices { get; set; }
        public IPackageChooser PackageChooser { get; set; }

        private void PackageMetadataEditor_IsVisibleChanged(object sender, DependencyPropertyChangedEventArgs e)
        {
            if (Visibility == Visibility.Visible)
            {
                ClearFrameworkAssemblyTextBox();
                ClearFilteredAssemblyReferenceTextBox();
                PrepareBindings();
            }
        }

        private void PrepareBindings()
        {
            var viewModel = (PackageViewModel) DataContext;

            _dependencySets = viewModel.PackageMetadata.DependencySets;

            _frameworkAssemblies =
                new ObservableCollection<FrameworkAssemblyReference>(viewModel.PackageMetadata.FrameworkAssemblies);
            FrameworkAssembliesList.ItemsSource = _frameworkAssemblies;

            _filteredAssemblyReferences =
                new ObservableCollection<string>(viewModel.PackageMetadata.PackageAssemblyReferences.Select(f => f.File));
            AssemblyReferencesList.ItemsSource = _filteredAssemblyReferences;
        }

        private void ClearFrameworkAssemblyTextBox()
        {
            _newFrameworkAssembly = new EditableFrameworkAssemblyReference();
            NewAssemblyName.DataContext = NewSupportedFramework.DataContext = _newFrameworkAssembly;
        }

        private void ClearFilteredAssemblyReferenceTextBox()
        {
            NewReferenceFileName.Text = String.Empty;
        }

        private void PopulateLanguagesForLanguageBox()
        {
            LanguageBox.ItemsSource =
                CultureInfo.GetCultures(CultureTypes.SpecificCultures)
                    .Select(c => c.Name)
                    .OrderBy(p => p, StringComparer.OrdinalIgnoreCase);
        }

        private void PopulateFrameworkAssemblyNames()
        {
            const string fxAssemblyPath = "Resources/fxAssemblies.txt";
            if (File.Exists(fxAssemblyPath))
            {
                try
                {
                    NewAssemblyName.ItemsSource = File.ReadAllLines(fxAssemblyPath);
                }
                catch (Exception)
                {
                    // ignore exception
                }
            }
        }

        private void RemoveFrameworkAssemblyButtonClicked(object sender, RoutedEventArgs e)
        {
            var button = (Button) sender;
            var item = (FrameworkAssemblyReference) button.DataContext;

            _frameworkAssemblies.Remove(item);
        }

        private void RemoveFilteredAssemblyReferenceClicked(object sender, RoutedEventArgs e)
        {
            var button = (Button) sender;
            var reference = (string) button.DataContext;
            _filteredAssemblyReferences.Remove(reference);
        }

        private void AddFrameworkAssemblyButtonClicked(object sender, RoutedEventArgs args)
        {
            AddPendingFrameworkAssembly();
        }

        // return true = pending assembly added
        // return false = pending assembly is invalid
        // return null = no pending asesmbly
        private bool? AddPendingFrameworkAssembly()
        {
            if (String.IsNullOrEmpty(NewAssemblyName.Text) &&
                String.IsNullOrEmpty(NewSupportedFramework.Text))
            {
                return null;
            }

            string displayString = NewSupportedFramework.Text.Trim();

            if (!NewFrameworkAssembly.UpdateSources())
            {
                return false;
            }
            _frameworkAssemblies.Add(_newFrameworkAssembly.AsReadOnly(displayString));

            // after framework assembly is added, clear the textbox
            ClearFrameworkAssemblyTextBox();

            return true;
        }

        private void AddReferenceFileNameClicked(object sender, RoutedEventArgs e)
        {
            string file = NewReferenceFileName.Text.Trim();
            _filteredAssemblyReferences.Add(file);

            // after reference name is added, clear the textbox
            ClearFilteredAssemblyReferenceTextBox();
        }

        private void EditDependenciesButtonClicked(object sender, RoutedEventArgs e)
        {
            var viewModel = (PackageViewModel)DataContext;

            var editor = new PackageDependencyEditor(_dependencySets)
            {
                Owner = Window.GetWindow(this),
                PackageChooser = PackageChooser
            };
            var result = editor.ShowDialog();
            if (result == true)
            {
                _dependencySets = editor.GetEditedDependencySets();
            }
        }

        #region IPackageEditorService

        void IPackageEditorService.BeginEdit()
        {
            PackageMetadataGroup.BeginEdit();
        }

        void IPackageEditorService.CancelEdit()
        {
            PackageMetadataGroup.CancelEdit();
        }

        bool IPackageEditorService.CommitEdit()
        {
            bool? addPendingAssembly = AddPendingFrameworkAssembly();
            if (addPendingAssembly == false)
            {
                return false;
            }

            bool valid = PackageMetadataGroup.CommitEdit();
            if (valid)
            {
                var viewModel = (PackageViewModel) DataContext;
                viewModel.PackageMetadata.DependencySets = _dependencySets;
                _frameworkAssemblies.CopyTo(viewModel.PackageMetadata.FrameworkAssemblies);
                _filteredAssemblyReferences.Distinct().Select(s => new AssemblyReference(s)).CopyTo(
                    viewModel.PackageMetadata.PackageAssemblyReferences);
            }

            return valid;
        }

        void IPackageEditorService.AddAssemblyReference(string name)
        {
            if (!String.IsNullOrWhiteSpace(name))
            {
                _filteredAssemblyReferences.Add(name);
            }
        }

        #endregion
    }
}