using System;
using System.Collections.ObjectModel;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net.NetworkInformation;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
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
        private EditablePackageDependency _newPackageDependency;
        private ObservableCollection<PackageDependency> _packageDependencies;

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
                ClearDependencyTextBox();
                ClearFrameworkAssemblyTextBox();
                ClearFilteredAssemblyReferenceTextBox();
                PrepareBindings();
            }
        }

        private void PrepareBindings()
        {
            var viewModel = (PackageViewModel) DataContext;

            _packageDependencies = new ObservableCollection<PackageDependency>(viewModel.PackageMetadata.Dependencies);
            DependencyList.ItemsSource = _packageDependencies;

            _frameworkAssemblies =
                new ObservableCollection<FrameworkAssemblyReference>(viewModel.PackageMetadata.FrameworkAssemblies);
            FrameworkAssembliesList.ItemsSource = _frameworkAssemblies;

            _filteredAssemblyReferences =
                new ObservableCollection<string>(viewModel.PackageMetadata.PackageAssemblyReferences.Select(f => f.File));
            AssemblyReferencesList.ItemsSource = _filteredAssemblyReferences;
        }

        private void ClearDependencyTextBox()
        {
            _newPackageDependency = new EditablePackageDependency();
            NewDependencyId.DataContext = NewDependencyVersion.DataContext = _newPackageDependency;
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
                CultureInfo.GetCultures(CultureTypes.SpecificCultures).Select(c => c.Name).OrderBy(p => p,
                                                                                                   StringComparer.
                                                                                                       OrdinalIgnoreCase);
        }

        private void PopulateFrameworkAssemblyNames()
        {
            string fxAssemblyPath = "Resources/fxAssemblies.txt";
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

        private void RemoveDependencyButtonClicked(object sender, RoutedEventArgs e)
        {
            var button = (Button) sender;
            var item = (PackageDependency) button.DataContext;

            _packageDependencies.Remove(item);
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

        private void AddDependencyButtonClicked(object sender, RoutedEventArgs e)
        {
            if (!NewPackageDependencyGroup.UpdateSources())
            {
                return;
            }

            _packageDependencies.Add(_newPackageDependency.AsReadOnly());

            // after dependency is added, clear the textbox
            ClearDependencyTextBox();
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

        private void AddFrameworkAssemblyButtonClicked(object sender, RoutedEventArgs args)
        {
            BindingExpression bindingExpression = NewSupportedFramework.GetBindingExpression(TextBox.TextProperty);
            if (!bindingExpression.ValidateWithoutUpdate())
            {
                return;
            }

            if (bindingExpression.HasError)
            {
                return;
            }

            string displayString = NewSupportedFramework.Text.Trim();

            bindingExpression.UpdateSource();
            _frameworkAssemblies.Add(_newFrameworkAssembly.AsReadOnly(displayString));

            // after framework assembly is added, clear the textbox
            ClearFrameworkAssemblyTextBox();
        }

        private void AddReferenceFileNameClicked(object sender, RoutedEventArgs e)
        {
            string file = NewReferenceFileName.Text.Trim();
            _filteredAssemblyReferences.Add(file);

            // after reference name is added, clear the textbox
            ClearFilteredAssemblyReferenceTextBox();
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
            bool valid = PackageMetadataGroup.CommitEdit();
            if (valid)
            {
                var viewModel = (PackageViewModel) DataContext;
                _packageDependencies.CopyTo(viewModel.PackageMetadata.Dependencies);
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