using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.Composition;
using System.Diagnostics.CodeAnalysis;
using NuGet;
using NuGetPackageExplorer.Types;

namespace PackageExplorerViewModel
{
    [Export(typeof(IPackageViewModelFactory))]
    public class PackageViewModelFactory : IPackageViewModelFactory
    {
        private readonly Lazy<PluginManagerViewModel> _pluginManagerViewModel;

        public PackageViewModelFactory()
        {
            _pluginManagerViewModel = new Lazy<PluginManagerViewModel>(
                () => new PluginManagerViewModel(PluginManager, UIServices, PackageChooser, PackageDownloader));
        }

        [Import]
        public IMruManager MruManager { get; set; }

        [Import]
        public IUIServices UIServices { get; set; }

        [Import]
        public Lazy<IPackageEditorService> EditorService { get; set; }

        [Import]
        public ISettingsManager SettingsManager { get; set; }

        [Import(typeof(IPluginManager))]
        public IPluginManager PluginManager { get; set; }

        [Import(typeof(IPackageChooser))]
        public IPackageChooser PackageChooser { get; set; }

        [Import(typeof(IPackageDownloader))]
        public IPackageDownloader PackageDownloader { get; set; }

        [SuppressMessage("Microsoft.Design", "CA1002:DoNotExposeGenericLists"),
         SuppressMessage("Microsoft.Usage", "CA2227:CollectionPropertiesShouldBeReadOnly"),
         SuppressMessage("Microsoft.Design", "CA1006:DoNotNestGenericTypesInMemberSignatures")]
        [ImportMany(AllowRecomposition = true)]
        public List<Lazy<IPackageContentViewer, IPackageContentViewerMetadata>> ContentViewerMetadata { get; set; }

        [SuppressMessage("Microsoft.Design", "CA1006:DoNotNestGenericTypesInMemberSignatures"),
         SuppressMessage("Microsoft.Design", "CA1002:DoNotExposeGenericLists"),
         SuppressMessage("Microsoft.Usage", "CA2227:CollectionPropertiesShouldBeReadOnly")]
        [ImportMany(AllowRecomposition = true)]
        public List<Lazy<IPackageRule>> PackageRules { get; set; }

        #region IPackageViewModelFactory Members

        public PackageViewModel CreateViewModel(IPackage package, string packageSource)
        {
            return new PackageViewModel(
                package,
                packageSource,
                MruManager,
                UIServices,
                EditorService.Value,
                SettingsManager,
                ContentViewerMetadata,
                PackageRules);
        }

        public PackageChooserViewModel CreatePackageChooserViewModel(string fixedPackageSource)
        {
            var model = new PackageChooserViewModel(
                new MruPackageSourceManager(new PackageSourceSettings(SettingsManager)),
                SettingsManager.ShowPrereleasePackages,
                SettingsManager.AutoLoadPackages,
                fixedPackageSource);
            model.PropertyChanged += OnPackageChooserViewModelPropertyChanged;
            return model;
        }

        public PluginManagerViewModel CreatePluginManagerViewModel()
        {
            return _pluginManagerViewModel.Value;
        }

        #endregion

        private void OnPackageChooserViewModelPropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            var model = (PackageChooserViewModel)sender;

            if (e.PropertyName == "ShowPrereleasePackages")
            {
                SettingsManager.ShowPrereleasePackages = model.ShowPrereleasePackages;
            }
            else if (e.PropertyName == "AutoLoadPackages")
            {
                SettingsManager.AutoLoadPackages = model.AutoLoadPackages;
            }
        }
    }
}