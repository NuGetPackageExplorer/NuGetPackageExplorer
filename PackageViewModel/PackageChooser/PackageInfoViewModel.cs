using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Data.Services.Client;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Input;
using NuGet.Packaging;
using NuGetPe;

namespace PackageExplorerViewModel
{
    public class PackageInfoViewModel : ViewModelBase
    {
        private readonly IPackageRepository _repository;
        private bool _isLoading;
        private string _errorMessage;
        private bool _showingAllVersions;
        private PackageInfo _selectedPackage;
        private readonly PackageChooserViewModel _parentViewModel;
        private CancellationTokenSource _downloadCancelSource;
        private bool _hasFinishedLoading;

        public PackageInfoViewModel(
            PackageInfo info, 
            bool showPrereleasePackages,
            IPackageRepository repository,
            PackageChooserViewModel parentViewModel)
        {
            Debug.Assert(info != null);
            Debug.Assert(repository != null);

            LatestPackageInfo = info;
            ShowPrerelease = showPrereleasePackages;
            _repository = repository;
            _parentViewModel = parentViewModel;
            AllPackages = new ObservableCollection<PackageInfo>();

            ToggleAllVersionsCommand = new RelayCommand(OnToggleAllVersions, CanToggleAllVersions);
            OpenCommand = new RelayCommand(OnOpenPackage);
            DownloadCommand = new RelayCommand(OnDownloadPackage);
            CancelCommand = new RelayCommand(OnCancelDownload, CanCancelDownload);
        }

        public ObservableCollection<PackageInfo> AllPackages { get; private set; }

        public PackageInfo SelectedPackage
        {
            get
            {
                return _selectedPackage;
            }
            set
            {
                if (_selectedPackage != value)
                {
                    _selectedPackage = value;
                    OnPropertyChanged();
                }
            }
        }

        public PackageInfo EffectiveSelectedPackage
        {
            get
            {
                return ShowingAllVersions ? SelectedPackage : LatestPackageInfo;
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

        public string ErrorMessage
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
                var query = _repository.GetPackagesById(LatestPackageInfo.Id, ShowPrerelease);
                query = query.OrderByDescending(p => p.Published);

                var packageInfos = GetPackageInfos(query, _repository);

                PackageInfo[] packageInfoList = null;
                var resourceNotFoundError = false;
                try
                {
                    packageInfoList = await LoadData(packageInfos, _downloadCancelSource.Token);
                }
                catch (DataServiceQueryException ex)
                {
                    resourceNotFoundError = IsResourceNotFoundError(ex);
                    if (!resourceNotFoundError) throw;
                }

                // for a 404 error, use the legacy way to find packages by id,
                // which requires filtering pre-release packages after the fact
                if (resourceNotFoundError)
                {
                    query = ((DataServicePackageRepository)_repository).LegacyGetPackagesById(LatestPackageInfo.Id);
                    packageInfos = GetPackageInfos(query, _repository);
                    packageInfoList = await LoadData(packageInfos, _downloadCancelSource.Token);
                    if (!ShowPrerelease)
                    {
                        packageInfoList = Array.FindAll(packageInfoList, p => !p.IsPrerelease);
                    }
                }

                if (_repository is DataServicePackageRepository dataServiceRepository)
                {
                    foreach (var entity in packageInfoList)
                    {
                        entity.DownloadUrl = dataServiceRepository.GetReadStreamUri(entity);
                    }
                }

                foreach (var p in packageInfoList)
                {
                    p.ShowAll = true;
                }

                // now show packages
                AllPackages.AddRange(packageInfoList);

                HasFinishedLoading = true;
            }
            catch (OperationCanceledException)
            {
            }
            catch (Exception exception)
            {
                ErrorMessage = exception.GetBaseException().Message;
            }
            finally
            {
                _downloadCancelSource = null;
                IsLoading = false;
            }
        }

        private static bool IsResourceNotFoundError(DataServiceQueryException ex)
        {
            return ex.InnerException is DataServiceClientException &&
                ((DataServiceClientException)ex.InnerException).StatusCode == 404;
        }

        private static IQueryable<PackageInfo> GetPackageInfos(IQueryable<IPackage> query, IPackageRepository repository)
        {
            if (repository is DataServicePackageRepository)
            {
                return query.Cast<DataServicePackage>().Select(p => new PackageInfo
                    {
                        Id = p.Id,
                        Version = p.Version,
                        Authors = p.Authors,
                        DownloadCount = p.DownloadCount,
                        VersionDownloadCount = p.VersionDownloadCount,
                        PackageHash = p.PackageHash,
                        PackageSize = p.PackageSize,
                        Published = p.Published
                    });
            }
            else
            {
                return query.Cast<ZipPackage>().Select(p => new PackageInfo
                    {
                        Id = p.Id,
                        Version = p.Version.ToString(),
                        Authors = string.Join(", ", p.Authors),
                        DownloadCount = p.DownloadCount,
                        VersionDownloadCount = p.VersionDownloadCount,
                        PackageHash = p.PackageHash,
                        PackageSize = p.PackageSize,
                        DownloadUrl = new Uri(p.Source),
                        Published = p.Published,
                    });
            }
        }

        protected async Task<PackageInfo[]> LoadData(IQueryable<PackageInfo> query, CancellationToken token)
        {
            PackageInfo[] results;

            if (query is DataServiceQuery<PackageInfo> dataServiceQuery)
            {
                dataServiceQuery = dataServiceQuery.AddQueryOption("semVerLevel", "2.0.0");
                var queryResponse =
                    await Task.Factory.FromAsync<IEnumerable<PackageInfo>>(dataServiceQuery.BeginExecute(null, null), dataServiceQuery.EndExecute);

                token.ThrowIfCancellationRequested();
                results = queryResponse.ToArray();
            }
            else
            {
                results = await Task.Run((Func<PackageInfo[]>)query.ToArray, token);
            }

            // sort by Version descending
            Array.Sort(results, (a, b) => b.SemanticVersion.CompareTo(a.SemanticVersion));

            return results;
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
    }
}