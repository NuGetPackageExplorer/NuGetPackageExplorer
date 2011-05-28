using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Input;
using NuGet;
using NuGetPackageExplorer.Types;

namespace PackageExplorerViewModel {
    public class PackageChooserViewModel : ViewModelBase {
        private const int ShowAllVersionsPageSize = 7;
        private const int ShowLatestVersionPageSize = 15;
        private const int PageBuffer = 30;
        private DataServicePackageRepository _packageRepository;
        private IQueryContext<PackageInfo> _currentQuery;
        private string _currentSearch;
        private IMruPackageSourceManager _packageSourceManager;
        private readonly DataServicePackageRepositoryFactory _packageRepositoryFactory;

        public PackageChooserViewModel(IMruPackageSourceManager packageSourceManager, IProxyService proxyService, bool showLatestVersion) {
            if (null == packageSourceManager) {
                throw new ArgumentNullException("packageSourceManager");
            }
            if (null == proxyService) {
                throw new ArgumentNullException("proxyService");
            }

            _showLatestVersion = showLatestVersion;
            Packages = new ObservableCollection<PackageInfo>();
            SortCommand = new RelayCommand<string>(Sort, column => TotalPackageCount > 0);
            SearchCommand = new RelayCommand<string>(Search, CanSearch);
            NavigationCommand = new RelayCommand<string>(NavigationCommandExecute, NavigationCommandCanExecute);
            LoadedCommand = new RelayCommand(() => Sort("VersionDownloadCount", ListSortDirection.Descending));
            ChangePackageSourceCommand = new RelayCommand<string>(ChangePackageSource);
            _packageRepositoryFactory = new DataServicePackageRepositoryFactory(proxyService);
            _packageSourceManager = packageSourceManager;
        }

        public event EventHandler LoadPackagesCompleted = delegate { };

        private string _sortColumn;

        public string SortColumn {
            get { return _sortColumn; }

            set {
                if (_sortColumn != value) {
                    _sortColumn = value;
                    OnPropertyChanged("SortColumn");
                }
            }
        }

        private ListSortDirection _sortDirection;

        public ListSortDirection SortDirection {
            get { return _sortDirection; }
            set {
                if (_sortDirection != value) {
                    _sortDirection = value;
                    OnPropertyChanged("SortDirection");
                }
            }
        }

        private bool _isEditable = true;

        public bool IsEditable {
            get {
                return _isEditable;
            }
            set {
                if (_isEditable != value) {
                    _isEditable = value;
                    OnPropertyChanged("IsEditable");
                    NavigationCommand.RaiseCanExecuteChanged();
                }
            }
        }

        private bool _showLatestVersion;

        public bool ShowLatestVersion {
            get {
                return _showLatestVersion;
            }
            set {
                if (_showLatestVersion != value) {
                    _showLatestVersion = value;
                    OnPropertyChanged("ShowLatestVersion");

                    ResetPackageRepository();
                    LoadPackages();
                }
            }
        }

        /// <summary>
        /// This method needs to be run on background thread so as not to block UI thread
        /// </summary>
        /// <returns></returns>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Design", "CA1031:DoNotCatchGeneralExceptionTypes")]
        private DataServicePackageRepository GetPackageRepository() {
            if (_packageRepository == null) {
                _packageRepository = _packageRepositoryFactory.CreateRepository(PackageSource);
            }
            return _packageRepository;
        }

        private void ResetPackageRepository() {
            _packageRepository = null;
        }

        public ObservableCollection<string> PackageSources {
            get { return _packageSourceManager.PackageSources; }
        }

        public string PackageSource {
            get {
                return _packageSourceManager.ActivePackageSource;
            }
            private set {
                _packageSourceManager.ActivePackageSource = value;
                OnPropertyChanged("PackageSource");
            }
        }

        private int _totalPackageCount;

        public int TotalPackageCount {
            get { return _totalPackageCount; }
            private set {
                if (_totalPackageCount != value) {
                    _totalPackageCount = value;
                    OnPropertyChanged("TotalPackageCount");
                }
            }
        }

        private int _beginPackage;

        public int BeginPackage {
            get { return _beginPackage; }
            private set {
                if (_beginPackage != value) {
                    _beginPackage = value;
                    OnPropertyChanged("BeginPackage");
                }
            }
        }

        private int _endPackage;

        public int EndPackage {
            get { return _endPackage; }
            private set {
                if (_endPackage != value) {
                    _endPackage = value;
                    OnPropertyChanged("EndPackage");
                }
            }
        }

        private string _statusContent;

        public string StatusContent {
            get { return _statusContent; }
            set {
                if (_statusContent != value) {
                    _statusContent = value;
                    OnPropertyChanged("StatusContent");
                }
            }
        }

        private bool _hasError;

        public bool HasError {
            get {
                return _hasError;
            }
            set {
                if (_hasError != value) {
                    _hasError = value;
                    OnPropertyChanged("HasError");
                }
            }
        }

        public ObservableCollection<PackageInfo> Packages { get; private set; }

        public RelayCommand<string> NavigationCommand { get; private set; }
        public ICommand SortCommand { get; private set; }
        public ICommand SearchCommand { get; private set; }
        public ICommand LoadedCommand { get; private set; }
        public ICommand ChangePackageSourceCommand { get; private set; }

        internal void LoadPage() {
            Debug.Assert(_currentQuery != null);

            var uiScheduler = TaskScheduler.FromCurrentSynchronizationContext();

            ShowMessage(Resources.LoadingMessage, false);
            IsEditable = false;

            Task.Factory.StartNew<IList<PackageInfo>>(
                QueryPackages, CancellationToken.None, TaskCreationOptions.None, TaskScheduler.Default).ContinueWith(
                result => {
                    if (result.IsFaulted) {
                        AggregateException exception = result.Exception;
                        ShowMessage((exception.InnerException ?? exception).Message, true);
                        ClearPackages();
                    }
                    else if (!result.IsCanceled) {
                        ShowPackages(result.Result, _currentQuery.TotalItemCount, _currentQuery.BeginPackage, _currentQuery.EndPackage);
                        ShowMessage(String.Empty, false);
                    }

                    IsEditable = true;

                    RaiseLoadPackagesCompletedEvent();
                },
                uiScheduler);
        }

        private void RaiseLoadPackagesCompletedEvent() {
            LoadPackagesCompleted(this, EventArgs.Empty);
        }

        private IList<PackageInfo> QueryPackages() {
            IList<PackageInfo> result = _currentQuery.GetItemsForCurrentPage().ToList();
            var repository = GetPackageRepository();
            foreach (PackageInfo entity in result) {
                entity.DownloadUrl = repository.GetReadStreamUri(entity);
            }
            return result;
        }

        private void LoadPackages() {
            ShowMessage(Resources.ConnectingMessage, false);

            IsEditable = false;
            TaskScheduler uiScheduler = TaskScheduler.FromCurrentSynchronizationContext();
            Task.Factory.StartNew<DataServicePackageRepository>(GetPackageRepository).ContinueWith(
                task => {
                    DataServicePackageRepository repository = task.Result;
                    if (repository == null) {
                        IsEditable = true;
                        ShowMessage(String.Empty, false);
                        ClearPackages();
                        return;
                    }

                    var query = repository.GetPackages();
                    if (!String.IsNullOrEmpty(_currentSearch)) {
                        query = query.Find(_currentSearch.Split(' '));
                    }

                    switch (SortColumn) {
                        case "Id":
                            query = SortDirection == ListSortDirection.Descending ? query.OrderByDescending(p => p.Id) : query.OrderBy(p => p.Id);
                            break;

                        case "Authors":
                            query = SortDirection == ListSortDirection.Descending ? query.OrderByDescending(p => p.Authors).ThenBy(p => p.Id) : query.OrderBy(p => p.Authors).ThenBy(p => p.Id);
                            break;

                        case "VersionDownloadCount":
                            query = SortDirection == ListSortDirection.Descending ? query.OrderByDescending(p => p.DownloadCount).ThenBy(p => p.Id) : query.OrderBy(p => p.DownloadCount).ThenBy(p => p.Id);
                            break;

                        case "Rating":
                            query = SortDirection == ListSortDirection.Descending ? query.OrderByDescending(p => p.Rating).ThenBy(p => p.Id) : query.OrderBy(p => p.Rating).ThenBy(p => p.Id);
                            break;

                        default:
                            query = query.OrderByDescending(p => p.DownloadCount).ThenBy(p => p.Id);
                            break;
                    }

                    IQueryable<PackageInfo> filteredQuery;

                    if (ShowLatestVersion) {
                        filteredQuery = query.
                            Where(p => p.IsLatestVersion).
                            Select(p => new PackageInfo {
                                Id = p.Id,
                                Version = p.Version,
                                Authors = p.Authors,
                                Rating = p.Rating,
                                DownloadCount = p.DownloadCount,
                                PackageHash = p.PackageHash
                            });

                        _currentQuery = new ShowLatestVersionQueryContext<PackageInfo>(
                            filteredQuery, 
                            ShowLatestVersionPageSize);
                    }
                    else {
                        /* show all versions */
                        filteredQuery = query.Select(p => new PackageInfo {
                            Id = p.Id,
                            Version = p.Version,
                            Authors = p.Authors,
                            Rating = p.Rating,
                            VersionRating = p.VersionRating,
                            VersionDownloadCount = p.VersionDownloadCount,
                            PackageHash = p.PackageHash
                        });

                        _currentQuery = new ShowAllVersionsQueryContext<PackageInfo>(
                            filteredQuery,
                            ShowAllVersionsPageSize,
                            PageBuffer,
                            PackageInfoEqualityComparer.Instance);
                    }
                    
                    LoadPage();
                },
                CancellationToken.None,
                TaskContinuationOptions.OnlyOnRanToCompletion,
                uiScheduler
            );
        }

        private void Search(string searchTerm) {
            if (_currentSearch != searchTerm) {
                _currentSearch = searchTerm;
                LoadPackages();
            }
        }

        private bool CanSearch(string searchTerm) {
            return IsEditable;
        }

        private void Sort(string column) {
            Sort(column, null);
        }

        private void Sort(string column, ListSortDirection? direction) {
            if (column == "Version") {
                // we can't sort Version
                return;
            }

            if (SortColumn == column) {
                if (direction.HasValue) {
                    SortDirection = direction.Value;
                }
                else {
                    SortDirection = SortDirection == ListSortDirection.Ascending
                                        ? ListSortDirection.Descending
                                        : ListSortDirection.Ascending;
                }
            }
            else {
                SortColumn = column;
                SortDirection = direction ?? ListSortDirection.Ascending;
            }

            LoadPackages();
        }

        private void ChangePackageSource(string source) {
            if (PackageSource != source || _currentQuery == null) {
                PackageSource = source;
                // add the new source to MRU list
                _packageSourceManager.NotifyPackageSourceAdded(source);

                ResetPackageRepository();
                LoadPackages();
            }
        }

        private void ClearPackages() {
            ShowPackages(Enumerable.Empty<PackageInfo>(), 0, 0, 0);
        }

        private void ShowPackages(IEnumerable<PackageInfo> packages, int totalPackageCount, int beginPackage, int endPackage) {
            TotalPackageCount = totalPackageCount;
            BeginPackage = beginPackage;
            EndPackage = endPackage;

            Packages.Clear();
            Packages.AddRange(packages);

            NavigationCommand.RaiseCanExecuteChanged();
        }

        private void ShowMessage(string message, bool hasError) {
            StatusContent = message;
            HasError = hasError;
        }

        #region NavigationCommand

        private bool NavigationCommandCanExecute(string action) {
            if (!IsEditable) {
                return false;
            }

            switch (action) {
                case "First":
                    return CanMoveFirst();

                case "Previous":
                    return CanMovePrevious();

                case "Next":
                    return CanMoveNext();

                case "Last":
                    return CanMoveLast();

                default:
                    throw new ArgumentOutOfRangeException("action");
            }
        }

        private void NavigationCommandExecute(string action) {
            switch (action) {
                case "First":
                    MoveFirst();
                    break;

                case "Previous":
                    MovePrevious();
                    break;

                case "Next":
                    MoveNext();
                    break;

                case "Last":
                    MoveLast();
                    break;
            }
        }

        private void MoveLast() {
            bool canMoveLast = _currentQuery.MoveLast();
            if (canMoveLast) {
                LoadPage();
            }
        }

        private void MoveNext() {
            bool canMoveNext = _currentQuery.MoveNext();
            if (canMoveNext) {
                LoadPage();
            }
        }

        private void MovePrevious() {
            bool canMovePrevious = _currentQuery.MovePrevious();
            if (canMovePrevious) {
                LoadPage();
            }
        }

        private void MoveFirst() {
            _currentQuery.MoveFirst();
            LoadPage();
        }

        private bool CanMoveLast() {
            return EndPackage < TotalPackageCount && ShowLatestVersion;
        }

        private bool CanMoveNext() {
            return EndPackage < TotalPackageCount;
        }

        private bool CanMovePrevious() {
            return BeginPackage > 1;
        }

        private bool CanMoveFirst() {
            return BeginPackage > 1;
        }

        #endregion
    }
}
