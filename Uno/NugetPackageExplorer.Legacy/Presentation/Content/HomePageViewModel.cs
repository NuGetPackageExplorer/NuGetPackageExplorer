using System.ComponentModel.Composition;

using NuGet.Packaging.Core;
using NuGet.Versioning;

using NuGetPe;

using NupkgExplorer.Framework.MVVM;
using NupkgExplorer.Framework.Navigation;

using Windows.Storage.Pickers;

namespace NupkgExplorer.Presentation.Content
{
    public partial class HomePageViewModel : ViewModelBase
    {
        [Import]
        public NavigationService NavigationService { get; set; } = null!;

        public ICommand OpenLocalPackageCommand => GetCommand(OpenLocalPackage);
        public ICommand OpenRemotePackageCommand => GetCommand(OpenRemotePackage);
        public ICommand OpenTestPackageCommand => GetCommand(OpenTestPackage);

        public HomePageViewModel()
        {
            Title = $"Home | {NuGetPackageExplorer.Constants.AppName}";
            Location = "/";
        }

        public async Task OpenLocalPackage()
        {
            // TODO: move file interaction to DialogService or UIService
            var picker = new FileOpenPicker();
            picker.SuggestedStartLocation = PickerLocationId.Downloads;
            picker.FileTypeFilter.Add(Constants.PackageExtension);
            picker.FileTypeFilter.Add(Constants.SymbolPackageExtension);

            var file = await picker.PickSingleFileAsync();
            if (file != null)
            {
                var vm = await InspectPackageViewModel.CreateFromLocalPackage(file);
                if (vm != null)
                    NavigationService.NavigateTo(vm);
            }
        }

        public void OpenRemotePackage()
        {
            NavigationService.NavigateTo<FeedPackagePickerViewModel>();
        }

        public async Task OpenTestPackage()
        {
            var identity = new PackageIdentity("Uno.Core.Build", NuGetVersion.Parse("2.3.0"));
            var vm = await InspectPackageViewModel.CreateFromRemotePackage(identity);
            if (vm != null)
                NavigationService.NavigateTo(vm);
        }
    }
}
