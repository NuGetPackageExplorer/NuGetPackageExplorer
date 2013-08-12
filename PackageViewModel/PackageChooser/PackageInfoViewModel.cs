using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Data.Services.Client;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Input;
using NuGet;

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

            ToggleAllVersionsCommand = new RelayCommand(OnToggleAllVersions);
            OpenCommand = new RelayCommand(OnOpenPackage);
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

        public ICommand ToggleAllVersionsCommand { get; private set; }
        public ICommand OpenCommand { get; private set; }

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

        public async Task LoadPackages(CancellationToken token)
        {
            IsLoading = true;
            ErrorMessage = null;
            AllPackages.Clear();

            try
            {
                IQueryable<IPackage> query = _repository.GetPackagesById(LatestPackageInfo.Id, ShowPrerelease);
                query = query.OrderByDescending(p => p.Published);

                IQueryable<PackageInfo> packageInfos = GetPackageInfos(query, _repository);

                PackageInfo[] packageInfoList = await LoadData(packageInfos, token);

                var dataServiceRepository = _repository as DataServicePackageRepository;
                if (dataServiceRepository != null)
                {
                    foreach (PackageInfo entity in packageInfoList)
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
                IsLoading = false;
            }
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
                        Authors = String.Join(", ", p.Authors),
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

            var dataServiceQuery = query as DataServiceQuery<PackageInfo>;
            if (dataServiceQuery != null)
            {
                IEnumerable<PackageInfo> queryResponse =
                    await Task.Factory.FromAsync<IEnumerable<PackageInfo>>(dataServiceQuery.BeginExecute(null, null), dataServiceQuery.EndExecute);

                token.ThrowIfCancellationRequested();
                results = queryResponse.ToArray();
            }
            else
            {
                results = await Task.Run((Func<PackageInfo[]>)query.ToArray, token);
            }

            // sort by Version descending
            Array.Sort(results, (a, b) => b.Version.CompareTo(a.Version));

            return results;
        }

        private async void OnToggleAllVersions()
        {
            if (ShowingAllVersions)
            {
                ShowingAllVersions = false;
            }
            else
            {
                if (AllPackages.Count == 0)
                {
                    // only load packages the first time
                    await LoadPackages(CancellationToken.None);

                    if (AllPackages.Count > 0)
                    {
                        // after loading, select the first package because it is usually the latest package
                        //SelectedPackage = AllPackages[0];
                    }
                }

                ShowingAllVersions = true;
            }
        }

        private void OnOpenPackage()
        {
            _parentViewModel.OnOpenPackage();
        }
    }
}