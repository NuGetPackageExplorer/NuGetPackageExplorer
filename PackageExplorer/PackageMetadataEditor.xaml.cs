using System;
using System.Collections.ObjectModel;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net.NetworkInformation;
using System.Windows;
using System.Windows.Controls;
using NuGet;
using NuGetPackageExplorer.Types;
using PackageExplorerViewModel;

namespace PackageExplorer {
    /// <summary>
    /// Interaction logic for PackageMetadataEditor.xaml
    /// </summary>
    public partial class PackageMetadataEditor : UserControl, IPackageEditorService {

        private ObservableCollection<PackageDependency> _packageDependencies;
        private ObservableCollection<FrameworkAssemblyReference> _frameworkAssemblies;
        private EditablePackageDependency _newPackageDependency;
        private EditableFrameworkAssemblyReference _newFrameworkAssembly;

        public IUIServices UIServices { get; set; }
        public IPackageChooser PackageChooser { get; set; }

        public PackageMetadataEditor() {
            InitializeComponent();
            PopulateLanguagesForLanguageBox();
            PopulateFrameworkAssemblyNames();
        }

        private void PackageMetadataEditor_IsVisibleChanged(object sender, DependencyPropertyChangedEventArgs e) {
            if (this.Visibility == System.Windows.Visibility.Visible) {
                ClearDependencyTextBox();
                ClearFrameworkAssemblyTextBox();
                PrepareBindings();
            }
        }

        private void PrepareBindings() {
            var viewModel = (PackageViewModel)DataContext;

            _packageDependencies = new ObservableCollection<PackageDependency>(viewModel.PackageMetadata.Dependencies);
            DependencyList.ItemsSource = _packageDependencies;

            _frameworkAssemblies =
                new ObservableCollection<FrameworkAssemblyReference>(viewModel.PackageMetadata.FrameworkAssemblies);
            FrameworkAssembliesList.ItemsSource = _frameworkAssemblies;
        }

        private void ClearDependencyTextBox() {
            _newPackageDependency = new EditablePackageDependency();
            NewDependencyId.DataContext = NewDependencyVersion.DataContext = _newPackageDependency;
        }

        private void ClearFrameworkAssemblyTextBox() {
            _newFrameworkAssembly = new EditableFrameworkAssemblyReference();
            NewAssemblyName.DataContext = NewSupportedFramework.DataContext = _newFrameworkAssembly;
        }

        private void PopulateLanguagesForLanguageBox() {
            LanguageBox.ItemsSource = CultureInfo.GetCultures(CultureTypes.SpecificCultures).Select(c => c.Name).OrderBy(p => p, StringComparer.OrdinalIgnoreCase);
        }

        private void PopulateFrameworkAssemblyNames() {
            string fxAssemblyPath = "Resources/fxAssemblies.txt";
            if (File.Exists(fxAssemblyPath)) {
                try {
                    NewAssemblyName.ItemsSource = File.ReadAllLines(fxAssemblyPath);
                }
                catch (Exception) {
                    // ignore exception
                }
            }

        }

        private void RemoveDependencyButtonClicked(object sender, System.Windows.RoutedEventArgs e) {
            var button = (Button)sender;
            var item = (PackageDependency)button.DataContext;

            _packageDependencies.Remove(item);
        }

        private void RemoveFrameworkAssemblyButtonClicked(object sender, System.Windows.RoutedEventArgs e) {
            var button = (Button)sender;
            var item = (FrameworkAssemblyReference)button.DataContext;

            _frameworkAssemblies.Remove(item);
        }

        private void AddDependencyButtonClicked(object sender, System.Windows.RoutedEventArgs e) {
            var bindingExpression = NewDependencyId.GetBindingExpression(TextBox.TextProperty);
            if (bindingExpression.HasError) {
                return;
            }

            var bindingExpression2 = NewDependencyVersion.GetBindingExpression(TextBox.TextProperty);
            if (bindingExpression2.HasError) {
                return;
            }

            _packageDependencies.Add(_newPackageDependency.AsReadOnly());

            // after dependency is added, clear the textbox
            ClearDependencyTextBox();
        }

        private void SelectDependencyButtonClicked(object sender, System.Windows.RoutedEventArgs e) {
            if (!NetworkInterface.GetIsNetworkAvailable()) {
                UIServices.Show(
                    PackageExplorer.Resources.Resources.NoNetworkConnection,
                    MessageLevel.Warning);
                return;
            }

            var selectedPackage = PackageChooser.SelectPackage(null);
            if (selectedPackage != null) {
                _newPackageDependency.Id = selectedPackage.Id;
                _newPackageDependency.VersionSpec = VersionUtility.ParseVersionSpec(selectedPackage.Version.ToString());
            }
        }

        private void AddFrameworkAssemblyButtonClicked(object sender, RoutedEventArgs args) {
            var bindingExpression = NewSupportedFramework.GetBindingExpression(TextBox.TextProperty);
            if (!bindingExpression.ValidateWithoutUpdate()) {
                return;
            }

            if (bindingExpression.HasError) {
                return;
            }

            string displayString = NewSupportedFramework.Text;

            bindingExpression.UpdateSource();
            _frameworkAssemblies.Add(_newFrameworkAssembly.AsReadOnly(displayString));

            // after framework assembly is added, clear the textbox
            ClearFrameworkAssemblyTextBox();
        }

        #region IPackageEditorService

        void IPackageEditorService.BeginEdit() {
            PackageMetadataGroup.BeginEdit();
        }

        void IPackageEditorService.CancelEdit() {
            PackageMetadataGroup.CancelEdit();
        }

        bool IPackageEditorService.CommitEdit() {
            bool valid = PackageMetadataGroup.CommitEdit();
            if (valid) {
                var viewModel = (PackageViewModel)DataContext;
                _packageDependencies.CopyTo(viewModel.PackageMetadata.Dependencies);
                _frameworkAssemblies.CopyTo(viewModel.PackageMetadata.FrameworkAssemblies);
            }

            return valid;
        }

        #endregion
    }
}
