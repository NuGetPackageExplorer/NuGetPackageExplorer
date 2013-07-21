using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Data.Services.Client;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using NuGet;

namespace PackageExplorerViewModel
{
    internal class PackageVersionsViewModel : ViewModelBase
    {
        private readonly IPackageRepository _repository;
        private bool _isLoading;
        private string _errorMessage;

        public PackageVersionsViewModel(
            string packageId, 
            bool showPrereleasePackages,
            IPackageRepository repository)
        {
            Debug.Assert(!String.IsNullOrEmpty(packageId));
            Debug.Assert(repository != null);

            PackageId = packageId;
            ShowPrerelease = showPrereleasePackages;
            _repository = repository;
        }

        public ObservableCollection<PackageInfo> Packages { get; private set; }

        public string PackageId { get; private set; }

        public bool ShowPrerelease { get; private set; }

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
            Packages.Clear();

            try
            {
                IQueryable<IPackage> query = _repository.GetPackagesById(PackageId, ShowPrerelease);
                query = query.OrderByDescending(p => p.Published);

                IQueryable<PackageInfo> packageInfos = GetPackageInfos(query, _repository);

                PackageInfo[] packageInfoList = await LoadData(packageInfos, token).ConfigureAwait(continueOnCapturedContext: false);

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
                Packages.AddRange(packageInfoList);
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
            var dataServiceQuery = query as DataServiceQuery<PackageInfo>;
            if (dataServiceQuery != null)
            {
                IEnumerable<PackageInfo> queryResponse =
                    await Task.Factory.FromAsync<IEnumerable<PackageInfo>>(dataServiceQuery.BeginExecute(null, null), dataServiceQuery.EndExecute)
                          .ConfigureAwait(continueOnCapturedContext: false);

                token.ThrowIfCancellationRequested();
                return queryResponse.ToArray();
            }
            else
            {
                return await Task.Run((Func<PackageInfo[]>)query.ToArray, token)
                             .ConfigureAwait(continueOnCapturedContext: false);
            }
        }
    }
}
