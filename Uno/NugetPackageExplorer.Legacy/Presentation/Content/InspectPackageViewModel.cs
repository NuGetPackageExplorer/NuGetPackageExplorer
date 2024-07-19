using System;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Reactive.Linq;
using System.Resources;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Input;

using NuGet.Packaging;
using NuGet.Packaging.Core;
using NuGet.Packaging.Signing;
using NuGet.Versioning;

using NuGetPackageExplorer.Types;

using NuGetPe;

using NupkgExplorer.Client;
using NupkgExplorer.Framework.Extensions;
using NupkgExplorer.Framework.Navigation;
using NupkgExplorer.Presentation.Dialogs;
using NupkgExplorer.Presentation.Helpers;

using PackageExplorer;

using PackageExplorerViewModel;

using Uno.Disposables;
using Uno.Extensions;
using Uno.Logging;

using Windows.ApplicationModel.Core;
using Windows.Storage;
using Windows.Storage.Pickers;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

using Constants = NuGetPe.Constants;

namespace NupkgExplorer.Presentation.Content
{
    public class InspectPackageViewModel : Framework.MVVM.ViewModelBase
    {
        public PackageViewModel Package
        {
            get => GetProperty<PackageViewModel>();
            set => SetProperty(value);
        }
        public IPart SelectedContent
        {
            get => GetProperty<IPart>();
            set => SetProperty(value);
        }
        public IFile OpenedDocument
        {
            get => GetProperty<IFile>();
            set => SetProperty(value);
        }
        public string OpenedDocumentLanguage
        {
            get => GetProperty<string>();
            set => SetProperty(value);
        }
        public string? VersionRedirectWarningMessage
        {
            get => GetProperty<string?>();
            set => SetProperty(value);
        }

        public ICommand ViewMetadataSourceCommand => GetCommand(ViewMetadataSource);

        public ICommand DoubleClickCommand => GetCommand(DoubleClick);

        public ICommand CloseDocumentCommand => GetCommand(CloseDocument);

        public InspectPackageViewModel(PackageViewModel package, PackageIdentity? redirectedFrom = null)
        {
            if (package == null) throw new ArgumentNullException(nameof(package));

            Title = $"{package.PackageMetadata} | {NuGetPackageExplorer.Constants.AppName}";
            Location = $"/packages/{package.PackageMetadata.Id}/{(redirectedFrom?.Version ?? package.PackageMetadata.Version)}";
            Package = package;
            VersionRedirectWarningMessage = redirectedFrom?.Apply(x => $"The specified version {x.Version} was not found. You have been taken to version {package.PackageMetadata.Version}.");

            this.WhenAnyValue(x => x.SelectedContent)
                .OfType<IFile>()
                .Subscribe(x =>
                {
                    OpenedDocument = x;
                    Package.SelectedItem = x;

                    Package.ViewContentCommand.Execute(x);
                    OpenedDocumentLanguage = ((package.CurrentFileInfo is { } fi && fi.IsTextFile)
                        ? MonacoEditorLanguageHelper.MapFileNameToLanguage(fi.Name)
                        : default
                    ) ?? "plaintext";
                });
        }

        public static async Task<InspectPackageViewModel> CreateFromLocalPackage(StorageFile packageFile)
        {
            // since the file returned by OpenFilePicker cannot be opened by its path
            // we are copying the file to the browser storage
            var localFolder = ApplicationData.Current.LocalFolder;
            var folder = await localFolder.CreateFolderAsync("UnoNPE", CreationCollisionOption.OpenIfExists);
            var copy = await folder.CreateFileAsync(packageFile.Name, CreationCollisionOption.GenerateUniqueName);
            using (var st = await packageFile.OpenStreamForReadAsync())
            {
                using (var newFileWriteStream = await copy.OpenStreamForWriteAsync())
                {
                    await st.CopyToAsync(newFileWriteStream);
                    await newFileWriteStream.FlushAsync();
                }
                await st.DisposeAsync();
            }

            return await CreateFromLocalPackage(copy.Path, openOriginal: true);
        }
        public static async Task<InspectPackageViewModel> CreateFromLocalPackage(string packagePath, bool openOriginal = false)
        {
            var tempFile = packagePath;
            if (!openOriginal)
            {
                tempFile = Path.GetTempFileName();
                File.Copy(packagePath, tempFile, overwrite: true);
            }

            IPackage? package = null;
            var extension = Path.GetExtension(packagePath);
            if (Constants.PackageExtension.Equals(extension, StringComparison.OrdinalIgnoreCase) ||
                Constants.SymbolPackageExtension.Equals(extension, StringComparison.OrdinalIgnoreCase))
            {
                DiagnosticsClient.TrackPageView("View Existing Package");
#pragma warning disable CA2000 // Dispose objects before losing scope
                package = new ZipPackage(tempFile);
#pragma warning restore CA2000 // Dispose objects before losing scope
            }
            else if (Constants.ManifestExtension.Equals(extension, StringComparison.OrdinalIgnoreCase))
            {
                // fixme: possible issue with accessing files within the same folder
                DiagnosticsClient.TrackPageView("View Nuspec");
                using var str = ManifestUtility.ReadManifest(tempFile);
                var builder = new PackageBuilder(str, Path.GetDirectoryName(packagePath));
                package = builder.Build();
            }
            else
            {
                throw new InvalidOperationException("Unsupport file type: " + extension);
            }

            var factory = DefaultContainer.GetExportedValue<IPackageViewModelFactory>()!;
            var packageVM = await factory.CreateViewModel(package, packagePath, packagePath);
            var vm = new InspectPackageViewModel(packageVM);

            return vm;
        }

        public static async Task<InspectPackageViewModel> CreateFromRemotePackage(PackageIdentity identity, PackageIdentity? redirectedFrom = null)
        {
            if (identity == null) throw new ArgumentNullException(nameof(identity));

            // TODO: move load cache/download to the caller
            var factory = DefaultContainer.GetExportedValue<IPackageViewModelFactory>()!;

            if (MachineCache.Default.FindPackage(identity.Id, identity.Version) is ISignaturePackage package)
            {
                try
                {
                    if (typeof(InspectPackageViewModel).Log().IsEnabled(Microsoft.Extensions.Logging.LogLevel.Debug))
                    {
                        typeof(InspectPackageViewModel).Log().Debug("loading package from cache...");
                    }
                    var packageVM = await factory.CreateViewModel(package, package.Source, NuGetConstants.DefaultFeedUrl);
                    var vm = new InspectPackageViewModel(packageVM, redirectedFrom);

                    return vm;
                }
                catch (Exception e)
                {
                    if (typeof(InspectPackageViewModel).Log().IsEnabled(Microsoft.Extensions.Logging.LogLevel.Debug))
                    {
                        typeof(InspectPackageViewModel).Log().Error("failed to loading package from cache: ", e);
                        typeof(InspectPackageViewModel).Log().Error("recovering with download from remote: nuget.org");
                    }
                }
            }

            try
            {
                var dialog = DefaultContainer.GetExportedValue<DialogService>()!;
                using var cts = new CancellationDisposable();
                var progressVM = new DownloadProgressDialogViewModel(identity.Id, identity.Version.ToNormalizedString(), cts);

                var dialogTask = dialog.ShowAsync(cts.Token, progressVM);
                var downloadPackageTask = DownloadPackage();

                var completed = await Task.WhenAny(dialogTask, downloadPackageTask);
                if (completed == downloadPackageTask)
                {
                    var packageVM = await factory.CreateViewModel(downloadPackageTask.Result, downloadPackageTask.Result.Source, NuGetConstants.DefaultFeedUrl);
                    var vm = new InspectPackageViewModel(packageVM, redirectedFrom);

                    return vm;
                }
                else
                {
                    throw new OperationCanceledException();
                }
            }
            catch (AggregateException ae) when (ae.GetPossibleInnerException<HttpResponseExceptionWithStatusCode>() is { StatusCode: HttpStatusCode.NotFound } e)
            {
                throw new PackageNotFoundException($"Package '{identity.Id} {identity.Version}' not found");
            }

            Task<ISignaturePackage?> DownloadPackage()
            {
                var downloader = DefaultContainer.GetExportedValue<INuGetPackageDownloader>()!;
                var repository = PackageRepositoryFactory.CreateRepository(NuGetConstants.DefaultFeedUrl);

                return downloader.Download(repository, identity);
            }
        }

        public static async Task<InspectPackageViewModel> CreateFromRemotePackageWithFallback(PackageIdentity identity)
        {
            if (identity == null) throw new ArgumentNullException(nameof(identity));

            try
            {
                // CreateFromRemotePackage will fail without a version specified,
                // so we throw anticipatedly here, to let the catch block handle this
                // also because we don't want to re-enter the catch block if the default version failed us somehow
                if (!identity.HasVersion)
                {
                    // if no package found, this exception will be rethrown; otherwise, ignored
                    throw new PackageNotFoundException($"Package '{identity.Id}' not found");
                }

                return await CreateFromRemotePackage(identity);
            }
            catch (PackageNotFoundException)
            {
                // check if any version exists for this package
                var defaultVersion = await TryGetDefaultPackageVersion();
                if (defaultVersion == null) throw;

                // retry with that version
                return await CreateFromRemotePackage(
                    new PackageIdentity(identity.Id, NuGetVersion.Parse(defaultVersion)),
                    identity.HasVersion ? identity : default // providing alternative version is redirecting, not when it is unspecified
                );
            }

            async Task<string?> TryGetDefaultPackageVersion()
            {
                try
                {
                    var nuget = DefaultContainer.GetExportedValue<INugetEndpoint>();
                    var response = await nuget.ListVersions(identity.Id);
                    var version = // prefer stable version over pre-release (containing `-{tag}`) version
                        response.Content.Versions.LastOrDefault(x => !x.Contains('-', StringComparison.InvariantCultureIgnoreCase)) ??
                        response.Content.Versions.LastOrDefault();

                    return version;
                }
                catch (Exception)
                {
                    return null;
                }
            }
        }

        public void ViewMetadataSource()
        {
            DiagnosticsClient.TrackEvent("InspectPackage_ViewMetadataSource");

            var manifest = Package.CreatePackageMetadataFile();
            SelectedContent = new PackageFile(manifest, manifest.Name, Package.RootFolder);
        }

        public async Task DoubleClick()
        {
            if (SelectedContent is IFile file)
            {
                var picker = new FileSavePicker
                {
                    SuggestedFileName = file.Name,
                    SuggestedStartLocation = PickerLocationId.Downloads,
                };

                var saveFile = await picker.PickSaveFileAsync();
                if (saveFile != null)
                {
                    CachedFileManager.DeferUpdates(saveFile);

                    using (var saveStream = await saveFile.OpenStreamForWriteAsync())
                    using (var stream = file.GetStream())
                    {
                        await stream.CopyToAsync(saveStream);
                    }

                    await CachedFileManager.CompleteUpdatesAsync(saveFile);
                }
            }
        }

        public async Task CloseDocument()
        {
            var current = OpenedDocument;
            OpenedDocument = null;
            if (SelectedContent == current)
            {
                SelectedContent = null;
            }

            Package.ShowContentViewer = false;
            Package.CurrentFileInfo = null;

            await Task.CompletedTask;
        }
    }
}
