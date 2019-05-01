using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Input;
using NuGet.Common;
using NuGet.Packaging;
using NuGet.Protocol;
using NuGet.Protocol.Core.Types;
using NuGetPe;

namespace PackageExplorerViewModel
{
    public class PackageInfoViewModel : ViewModelBase
    {
        private readonly SourceRepository _repository;
        private readonly FeedType _feedType;
        private bool _isLoading;
        private string? _errorMessage;
        private bool _showingAllVersions;
        private PackageInfo? _selectedPackage;
        private readonly PackageChooserViewModel _parentViewModel;
        private CancellationTokenSource? _downloadCancelSource;
        private bool _hasFinishedLoading;
        private readonly Func<Task<IEnumerable<VersionInfo>>> _versionInfos = () => Task.FromResult(Enumerable.Empty<VersionInfo>());

        public PackageInfoViewModel(
            PackageInfo info,
            bool showPrereleasePackages,
            SourceRepository repository,
            FeedType feedType,
            PackageChooserViewModel parentViewModel)
        {
            Debug.Assert(info != null);
            Debug.Assert(repository != null);

            LatestPackageInfo = info;
            SelectedPackage = info;
            ShowPrerelease = showPrereleasePackages;
            _repository = repository;
            _parentViewModel = parentViewModel;
            _feedType = feedType;
            AllPackages = new ObservableCollection<PackageInfo>();

            ToggleAllVersionsCommand = new RelayCommand(OnToggleAllVersions, CanToggleAllVersions);
            OpenCommand = new RelayCommand(OnOpenPackage);
            DownloadCommand = new RelayCommand(OnDownloadPackage);
            CancelCommand = new RelayCommand(OnCancelDownload, CanCancelDownload);
        }

        public PackageInfoViewModel(
            IPackageSearchMetadata info,
            bool showPrereleasePackages,
            SourceRepository repository,
            FeedType feedType,
            PackageChooserViewModel parentViewModel)
            : this(CreatePackageInfo(info, feedType, null), showPrereleasePackages, repository, feedType, parentViewModel)
        {
            _versionInfos = info.GetVersionsAsync;
        }

        public ObservableCollection<PackageInfo> AllPackages { get; private set; }

        public PackageInfo? SelectedPackage
        {
            get
            {
                return _selectedPackage;
            }
            set
            {
                if (_selectedPackage != value)
                {
                    if (value != null)
                    {
                        _selectedPackage = value;
                    }
                    OnPropertyChanged();
                }
            }
        }

        public PackageInfo LatestPackageInfo { get; private set; }

        public bool ShowPrerelease { get; private set; }

        public RelayCommand ToggleAllVersionsCommand { get; private set; }
        public ICommand OpenCommand { get; private set; }
        public ICommand DownloadCommand { get; private set; }
        public ICommand CancelCommand { get; private set; }

        public bool ShowingAllVersionsList
        {
            get
            {
                return ShowingAllVersions && HasFinishedLoading;
            }
        }

        public bool ShowingAllVersions
        {
            get
            {
                return _showingAllVersions;
            }
            set
            {
                if (_showingAllVersions != value)
                {
                    _showingAllVersions = value;
                    OnPropertyChanged();
                    OnPropertyChanged("ShowingAllVersionsList");
                    ToggleAllVersionsCommand.RaiseCanExecuteChanged();
                }
            }
        }

        public bool HasFinishedLoading
        {
            get
            {
                return _hasFinishedLoading;
            }
            private set
            {
                if (_hasFinishedLoading != value)
                {
                    _hasFinishedLoading = value;
                    OnPropertyChanged();
                    OnPropertyChanged("ShowingAllVersionsList");
                }
            }
        }

        public bool IsLoading
        {
            get
            {
                return _isLoading;
            }
            set
            {
                if (_isLoading != value)
                {
                    _isLoading = value;
                    OnPropertyChanged();

                    // we don't need to raise this event because the Cancel button
                    // is only visible when IsLoading = true anyway.
                    //CancelCommand.RaiseCanExecuteChanged();

                    ToggleAllVersionsCommand.RaiseCanExecuteChanged();
                }
            }
        }

        public string? ErrorMessage
        {
            get
            {
                return _errorMessage;
            }
            set
            {
                if (_errorMessage != value)
                {
                    _errorMessage = value;
                    OnPropertyChanged();
                }
            }
        }

        private async Task LoadPackages()
        {
            if (IsLoading)
            {
                // prevent concurrent loading
                return;
            }

            IsLoading = true;
            ErrorMessage = null;
            AllPackages.Clear();
            _downloadCancelSource = new CancellationTokenSource();

            try
            {
                var versions = await _versionInfos();

                var packageMetadataResource = await _repository.GetResourceAsync<PackageMetadataResource>(_downloadCancelSource.Token);

                using (var sourceCacheContext = new SourceCacheContext())
                {
                    var query = await packageMetadataResource.GetMetadataAsync(LatestPackageInfo.Id, ShowPrerelease, ShowPrerelease, sourceCacheContext, NullLogger.Instance, _downloadCancelSource.Token);

                    query = query.OrderByDescending(p => p.Identity.Version);

                    // now show packages
                    AllPackages.AddRange(query.Select(p => CreatePackageInfo(p, _feedType, versions)));
                }

                HasFinishedLoading = true;
            }
            catch (OperationCanceledException)
            {
            }
            catch (Exception exception)
            {
                if (!(exception is FatalProtocolException) && !(exception is IOException) && !(exception is NullReferenceException))
                {
                    DiagnosticsClient.TrackException(exception);
                }
                ErrorMessage = exception.GetBaseException().Message;
            }
            finally
            {
                _downloadCancelSource = null;
                IsLoading = false;
            }
        }

        private bool CanToggleAllVersions()
        {
            return !IsLoading || ShowingAllVersions;
        }

        private async void OnToggleAllVersions()
        {
            if (ShowingAllVersions)
            {
                ShowingAllVersions = false;
            }
            else
            {
                if (AllPackages.Count == 0 && !HasFinishedLoading)
                {
                    // only load packages the first time
                    await LoadPackages();

                    if (AllPackages.Count > 0)
                    {
                        // after loading, select the first package because it is usually the latest package
                        SelectedPackage = AllPackages[0];
                    }
                }

                if (HasFinishedLoading)
                {
                    ShowingAllVersions = true;
                }
            }
        }

        private void OnOpenPackage()
        {
            _parentViewModel.OnOpenPackage();
        }

        private void OnDownloadPackage()
        {
            _parentViewModel.OnDownloadPackage();
        }

        private bool CanCancelDownload()
        {
            return IsLoading &&
                   _downloadCancelSource != null &&
                   !_downloadCancelSource.IsCancellationRequested;
        }

        private void OnCancelDownload()
        {
            if (_downloadCancelSource != null)
            {
                _downloadCancelSource.Cancel();
                IsLoading = false;
            }
        }

        internal void OnDeselected()
        {
            if (IsLoading)
            {
                OnCancelDownload();
            }

            if (ShowingAllVersions)
            {
                ShowingAllVersions = false;
            }
        }

        private static PackageInfo CreatePackageInfo(IPackageSearchMetadata packageSearchMetadata, FeedType feedType, IEnumerable<VersionInfo>? versionInfos)
        {
            var versionInfo = versionInfos?.FirstOrDefault(v => v.Version == packageSearchMetadata.Identity.Version);

            return new PackageInfo(packageSearchMetadata.Identity)
            {
                Authors = packageSearchMetadata.Authors,
                Published = packageSearchMetadata.Published,
                DownloadCount = (int)(versionInfo?.DownloadCount ?? packageSearchMetadata.DownloadCount.GetValueOrDefault()),
                IsRemotePackage = (feedType == FeedType.HttpV3 || feedType == FeedType.HttpV2),
                IsPrefixReserved = packageSearchMetadata.PrefixReserved,
                Description = packageSearchMetadata.Description,
                Tags = packageSearchMetadata.Tags,
                Summary = packageSearchMetadata.Summary,
                LicenseUrl = packageSearchMetadata.LicenseUrl?.ToString() ?? string.Empty,
                ProjectUrl = packageSearchMetadata.ProjectUrl?.ToString() ?? string.Empty,
                ReportAbuseUrl = packageSearchMetadata.ReportAbuseUrl?.ToString() ?? string.Empty,
                IconUrl = packageSearchMetadata.IconUrl?.ToString() ?? string.Empty
            };
        }
    }
}
