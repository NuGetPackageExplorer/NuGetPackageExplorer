using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.Composition;
using System.Diagnostics.CodeAnalysis;
using System.Threading.Tasks;
using NuGetPackageExplorer.Types;
using NuGetPe;

namespace PackageExplorerViewModel
{
    [Export(typeof(IPackageViewModelFactory))]
    public class PackageViewModelFactory : IPackageViewModelFactory
    {
        private readonly Lazy<PluginManagerViewModel> _pluginManagerViewModel;

#pragma warning disable CS8618 // Non-nullable field is uninitialized.
        public PackageViewModelFactory()
#pragma warning restore CS8618 // Non-nullable field is uninitialized.
        {
            _pluginManagerViewModel = new Lazy<PluginManagerViewModel>(
                () => new PluginManagerViewModel(PluginManager!, UIServices!, PackageChooser!, PackageDownloader!));
        }

        [Import]
        public IMruManager MruManager { get; set; }

        [Import]
        public IUIServices UIServices { get; set; }

        [Import]
        public Lazy<IPackageEditorService> EditorService { get; set; }

        [Import]
        public ISettingsManager SettingsManager { get; set; }

        [Import]
        public CredentialPublishProvider CredentialPublishProvider { get; set; }

        [Import(typeof(IPluginManager))]
        public IPluginManager PluginManager { get; set; }

        [Import(typeof(IPackageChooser))]
        public IPackageChooser PackageChooser { get; set; }

        [Import(typeof(INuGetPackageDownloader))]
        public INuGetPackageDownloader PackageDownloader { get; set; }

        [SuppressMessage("Microsoft.Usage", "CA2227:CollectionPropertiesShouldBeReadOnly")]
        [ImportMany(AllowRecomposition = true)]
        public List<Lazy<IPackageContentViewer, IPackageContentViewerMetadata>> ContentViewerMetadata { get; set; }

        [SuppressMessage("Microsoft.Usage", "CA2227:CollectionPropertiesShouldBeReadOnly")]
        [ImportMany(AllowRecomposition = true)]
        public List<Lazy<IPackageRule>> PackageRules { get; set; }

        #region IPackageViewModelFactory Members

        public async Task<PackageViewModel> CreateViewModel(IPackage package, string packagePath, string packageSource)
        {
#if !NETSTANDARD2_1 // UNO TODO: Use proper platform detection; wasm: System.Security.Cryptography.Encoding is not supported on this platform.
            // If it's a zip package, we need to load the verification data so it's ready for later
            if (package is ISignaturePackage zip)
            {
                await zip.LoadSignatureDataAsync();
            }
#endif

            var pvm = new PackageViewModel(
                package,
                packagePath,
                packageSource,
                MruManager,
                UIServices,
                EditorService.Value,
                SettingsManager,
                CredentialPublishProvider,
                ContentViewerMetadata,
                PackageRules);

            DiagnosticsClient.TrackEvent("PackageViewModelFactory_CreateViewModel", package, pvm.PublishedOnNuGetOrg);

            return pvm;
        }

        public PackageChooserViewModel CreatePackageChooserViewModel(string? fixedPackageSource)
        {
            var packageSourceSettings = new PackageSourceSettings(SettingsManager);
            var packageSourceManager = new MruPackageSourceManager(packageSourceSettings);
            var model = new PackageChooserViewModel(packageSourceManager,
                                                    UIServices,
                                                    SettingsManager.ShowPrereleasePackages,
                                                    fixedPackageSource);
            model.PropertyChanged += OnPackageChooserViewModelPropertyChanged;
            return model;
        }

        public PluginManagerViewModel CreatePluginManagerViewModel()
        {
            return _pluginManagerViewModel.Value;
        }

        #endregion

        private void OnPackageChooserViewModelPropertyChanged(object? sender, PropertyChangedEventArgs e)
        {
            var model = (PackageChooserViewModel)sender!;

            if (e.PropertyName == "ShowPrereleasePackages")
            {
                SettingsManager.ShowPrereleasePackages = model.ShowPrereleasePackages;
            }
        }
    }
}
