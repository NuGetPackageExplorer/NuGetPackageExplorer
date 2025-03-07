using System.ComponentModel.Composition;
using System.Reactive.Linq;

using NuGet.Packaging.Core;
using NuGet.Versioning;

using NupkgExplorer.Client;
using NupkgExplorer.Client.Data;
using NupkgExplorer.Framework.Extensions;
using NupkgExplorer.Framework.MVVM;
using NupkgExplorer.Framework.Navigation;

using Uno.Extensions;

namespace NupkgExplorer.Presentation.Content
{
    public partial class FeedPackagePickerViewModel : ViewModelBase
    {
        [Import]
        public NavigationService NavigationService { get; set; } = null!;

        public string? SearchTerm
        {
            get => GetProperty<string>();
            set => SetProperty(value);
        }
        public bool IncludePrerelease
        {
            get => GetProperty<bool>();
            set => SetProperty(value);
        }
        public PaginatedCollection<PackageData>? NugetPackages
        {
            get => GetProperty<PaginatedCollection<PackageData>>();
            set => SetProperty(value);
        }
        public PackageData? SelectedPackage
        {
            get => GetProperty<PackageData>();
            set => SetProperty(value);
        }
        public string[]? SelectedPackageVersions
        {
            get => GetProperty<string[]>();
            set => SetProperty(value);
        }
        public string? SelectedPackageVersion
        {
            get => GetProperty<string>();
            set => SetProperty(value);
        }

        public ICommand RefreshCommand => GetCommand(Refresh);
        public ICommand LoadPackageVersionsCommand => GetCommand(LoadPackageVersions);
        public ICommand OpenPackageFromFeedCommand => GetCommand(OpenPackageFromFeed);

        public FeedPackagePickerViewModel() : this(null) { }
        public FeedPackagePickerViewModel(string? searchTerm = null)
        {
            Title = $"Packages | {NuGetPackageExplorer.Constants.AppName}";
            Location = !string.IsNullOrWhiteSpace(searchTerm)
                ? $"/packages?q={Uri.EscapeDataString(searchTerm)}"
                : $"/packages";

            using (SuppressPropertyChangedNotifications())
            {
                SearchTerm = searchTerm!;
            }

            // auto refresh when SearchTerm changed
            this.WhenAnyValue(x => x.SearchTerm, x => x.IncludePrerelease)
                .DistinctUntilChanged()
                .Throttle(TimeSpan.FromMilliseconds(300))
                .SubscribeToCommand(RefreshCommand);

            this.WhenAnyValue(x => x.SelectedPackage)
                .Subscribe(package =>
                {
                    SelectedPackageVersion = null!;
                    SelectedPackageVersions = null!;
                });

            Task.Run(Refresh);
        }

        public Task Refresh()
        {
            var nuget = Container.GetExportedValue<INugetEndpoint>()!;

            var tcs = new TaskCompletionSource();
            SelectedPackage = null!;
            NugetPackages = new PaginatedCollection<PackageData>(
                async (start, size) =>
                {
                    var response = await nuget.Search(SearchTerm, skip: start, take: size, prerelease: IncludePrerelease);
                    return response.Content.Data;
                },
                pageSize: 25,
                tcs
            );
            return tcs.Task;
        }

        public async Task LoadPackageVersions()
        {
            if (SelectedPackage == null) throw new ArgumentNullException(nameof(SelectedPackage));

            async Task<IEnumerable<string>> GetVersions()
            {
                if (IncludePrerelease)
                {
                    var nuget = Container.GetExportedValue<INugetEndpoint>()!;
                    var response = await nuget.ListVersions(SelectedPackage.Id);

                    return response.Content.Versions;
                }
                else
                {
                    // PackageData.Versions contains only stable versions
                    return SelectedPackage.Versions.Select(x => x.Version);
                }
            }

            SelectedPackageVersions = [.. (await GetVersions()).Reverse()];
            SelectedPackageVersion =
                SelectedPackageVersions.FirstOrDefault(x => string.Equals(x, SelectedPackage.Version, StringComparison.InvariantCultureIgnoreCase)) ??
                SelectedPackageVersions.FirstOrDefault()!;
        }

        public async Task OpenPackageFromFeed(object? parameter)
        {
            try
            {
                // parameter is not null, when invoked from double-clicking on the listview
                var package = parameter as PackageData ?? SelectedPackage ?? throw new ArgumentNullException(nameof(SelectedPackage));
                var version = parameter is PackageData
                    ? package.Version // ignore any selected version, when double-clicking
                    : (SelectedPackageVersion ?? package.Version);
                var identity = new PackageIdentity(package.Id, NuGetVersion.Parse(version));
                var inspectVM = await InspectPackageViewModel.CreateFromRemotePackage(identity);
                if (inspectVM != null)
                    NavigationService.NavigateTo(inspectVM);
            }
            catch (Exception e)
            {
                Console.WriteLine("OpenPackageFromFeed exception " + e);
            }
        }
    }
}
