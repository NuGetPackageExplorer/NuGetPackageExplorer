using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Input;
using NuGet;

namespace PackageExplorerViewModel
{
    public sealed class PackageChooserViewModel : ViewModelBase, IDisposable
    {
        private const int ShowAllVersionsPageSize = 7;
        private const int ShowLatestVersionPageSize = 15;
        private const int PageBuffer = 30;
        private readonly string _fixedPackageSource;
        private int _beginPackage;
        private CancellationTokenSource _currentCancellationTokenSource;
        private IQueryContext<PackageInfo> _currentQuery;
        private string _currentSearch;
        private string _currentTypingSearch;
        private int _endPackage;
        private bool _hasError;
        private bool _isEditable = true;
        private IPackageRepository _packageRepository;
        private MruPackageSourceManager _packageSourceManager;
        private bool _showLatestVersion;
        private bool _showPrereleasePackages;
        private string _sortColumn;
        private ListSortDirection _sortDirection;
        private string _statusContent;
        private int _totalPackageCount;

        public PackageChooserViewModel(
            MruPackageSourceManager packageSourceManager,
            bool showLatestVersion,
            bool showUnlistedPackages,
            string fixedPackageSource)
        {
            if (packageSourceManager == null)
            {
                throw new ArgumentNullException("packageSourceManager");
            }

            _showLatestVersion = showLatestVersion;
            _showPrereleasePackages = showUnlistedPackages;
            _fixedPackageSource = fixedPackageSource;
            Packages = new ObservableCollection<PackageInfo>();
            SortCommand = new RelayCommand<string>(Sort, CanSort);
            SearchCommand = new RelayCommand<string>(Search, CanSearch);
            ClearSearchCommand = new RelayCommand(ClearSearch, CanClearSearch);
            NavigationCommand = new RelayCommand<string>(NavigationCommandExecute, NavigationCommandCanExecute);
            LoadedCommand = new RelayCommand(() => Sort("VersionDownloadCount", ListSortDirection.Descending));
            ChangePackageSourceCommand = new RelayCommand<string>(ChangePackageSource);
            CancelCommand = new RelayCommand(CancelCommandExecute, CanCancelCommandExecute);
            _packageSourceManager = packageSourceManager;
        }

        public string CurrentTypingSearch
        {
            get { return _currentTypingSearch; }
            set
            {
                if (_currentTypingSearch != value)
                {
                    _currentTypingSearch = value;
                    OnPropertyChanged("CurrentTypingSearch");
                }
            }
        }

        public string SortColumn
        {
            get { return _sortColumn; }

            set
            {
                if (_sortColumn != value)
                {
                    _sortColumn = value;
                    OnPropertyChanged("SortColumn");
                }
            }
        }

        public ListSortDirection SortDirection
        {
            get { return _sortDirection; }
            set
            {
                // IMPORTANT: must NOT check if (_sortDirection != value) here.
                // We want SortDirection property change event to raise even in that case so that
                // the UI can redraw the sort glyph when SortColumn property chagnes.
                _sortDirection = value;
                OnPropertyChanged("SortDirection");
            }
        }

        public bool IsEditable
        {
            get { return _isEditable; }
            set
            {
                if (_isEditable != value)
                {
                    _isEditable = value;
                    OnPropertyChanged("IsEditable");
                    NavigationCommand.RaiseCanExecuteChanged();
                }
            }
        }

        public bool ShowLatestVersion
        {
            get { return _showLatestVersion; }
            set
            {
                if (_showLatestVersion != value)
                {
                    _showLatestVersion = value;
                    OnPropertyChanged("ShowLatestVersion");
                    OnPropertyChanged("ShowAllVersions");

                    OnShowLatestVersionChanged();
                }
            }
        }

        public bool ShowAllVersions
        {
            get
            {
                return !ShowLatestVersion;
            }
        }

        public bool ShowPrereleasePackages
        {
            get { return _showPrereleasePackages; }
            set
            {
                if (_showPrereleasePackages != value)
                {
                    _showPrereleasePackages = value;
                    OnPropertyChanged("ShowPrereleasePackages");

                    OnShowPrereleasePackagesChange();
                }
            }
        }

        public ObservableCollection<string> PackageSources
        {
            get { return _packageSourceManager.PackageSources; }
        }

        public bool AllowsChangingPackageSource
        {
            get { return _fixedPackageSource == null; }
        }

        public string PackageSource
        {
            get { return _fixedPackageSource ?? _packageSourceManager.ActivePackageSource; }
            private set
            {
                if (_fixedPackageSource != null)
                {
                    throw new InvalidOperationException(
                        "Cannot set active package source when fixed package source is used.");
                }
                _packageSourceManager.ActivePackageSource = value;
                OnPropertyChanged("PackageSource");
            }
        }

        private CancellationTokenSource CurrentCancellationTokenSource
        {
            get { return _currentCancellationTokenSource; }
            set
            {
                _currentCancellationTokenSource = value;
                CancelCommand.RaiseCanExecuteChanged();
            }
        }

        public int TotalPackageCount
        {
            get { return _totalPackageCount; }
            private set
            {
                if (_totalPackageCount != value)
                {
                    _totalPackageCount = value;
                    OnPropertyChanged("TotalPackageCount");
                }
            }
        }

        public int BeginPackage
        {
            get { return _beginPackage; }
            private set
            {
                if (_beginPackage != value)
                {
                    _beginPackage = value;
                    OnPropertyChanged("BeginPackage");
                }
            }
        }

        public int EndPackage
        {
            get { return _endPackage; }
            private set
            {
                if (_endPackage != value)
                {
                    _endPackage = value;
                    OnPropertyChanged("EndPackage");
                }
            }
        }

        public string StatusContent
        {
            get { return _statusContent; }
            set
            {
                if (_statusContent != value)
                {
                    _statusContent = value;
                    OnPropertyChanged("StatusContent");
                }
            }
        }

        public bool HasError
        {
            get { return _hasError; }
            set
            {
                if (_hasError != value)
                {
                    _hasError = value;
                    OnPropertyChanged("HasError");
                }
            }
        }

        public ObservableCollection<PackageInfo> Packages { get; private set; }

        public RelayCommand<string> NavigationCommand { get; private set; }
        public ICommand SortCommand { get; private set; }
        public ICommand SearchCommand { get; private set; }
        public ICommand ClearSearchCommand { get; private set; }
        public ICommand LoadedCommand { get; private set; }
        public ICommand ChangePackageSourceCommand { get; private set; }
        public RelayCommand CancelCommand { get; private set; }

        #region IDisposable Members

        public void Dispose()
        {
            if (_packageSourceManager != null)
            {
                _packageSourceManager.Dispose();
                _packageSourceManager = null;
            }

            if (CurrentCancellationTokenSource != null)
            {
                CurrentCancellationTokenSource.Dispose();
            }
        }

        #endregion

        public event EventHandler LoadPackagesCompleted = delegate { };

        private void OnShowLatestVersionChanged()
        {
            if ((SortColumn == "LastUpdated" || SortColumn == "PackageSize") && !ShowLatestVersion)
            {
                Sort("Id", ListSortDirection.Ascending);
            }
            else
            {
                Sort(SortColumn, SortDirection);
            }
        }

        private void OnShowPrereleasePackagesChange()
        {
            Sort(SortColumn, SortDirection);
        }

        /// <summary>
        /// This method needs run on background thread so as not to block UI thread
        /// </summary>
        /// <returns></returns>
        [SuppressMessage("Microsoft.Design", "CA1031:DoNotCatchGeneralExceptionTypes")]
        private IPackageRepository GetPackageRepository(object state)
        {
            if (_packageRepository == null)
            {
                _packageRepository = PackageRepositoryFactory.CreateRepository(PackageSource);
            }

            var token = (CancellationToken)state;
            token.ThrowIfCancellationRequested();

            return _packageRepository;
        }

        private void ResetPackageRepository()
        {
            _packageRepository = null;
        }

        internal void LoadPage(CancellationToken token)
        {
            Debug.Assert(_currentQuery != null);

            TaskScheduler uiScheduler;
            try
            {
                uiScheduler = TaskScheduler.FromCurrentSynchronizationContext();
            }
            catch (InvalidOperationException)
            {
                uiScheduler = TaskScheduler.Current;
            }

            IsEditable = false;
            ShowMessage(Resources.LoadingMessage, false);
            ClearPackages(isErrorCase: false);

            if (token == CancellationToken.None)
            {
                CurrentCancellationTokenSource = new CancellationTokenSource();
                token = CurrentCancellationTokenSource.Token;
            }

            CancellationTokenSource usedTokenSource = CurrentCancellationTokenSource;

            // IMPORTANT: Call StartNew() with TaskScheduler.Default here to make sure 
            // QueryPackages() is scheduled on background thread. This is because this method
            // can be called from UI TaskScheduler from the LoadPackages() method
            Task.Factory.StartNew<IList<PackageInfo>>(
                QueryPackages, token, token, TaskCreationOptions.None, TaskScheduler.Default)
                .ContinueWith(
                    task =>
                    {
                        Exception exception = task.Exception;
                        if (usedTokenSource != CurrentCancellationTokenSource)
                        {
                            // this mean this request has already been canceled. No need to process this request anymore.
                            return;
                        }

                        if (task.IsFaulted)
                        {
                            ShowMessage(exception.GetBaseException().Message, true);
                            ClearPackages(isErrorCase: true);
                        }
                        else if (task.IsCanceled)
                        {
                            ClearMessage();
                            UpdatePageNumber(_currentQuery.TotalItemCount, _currentQuery.BeginPackage,
                                             _currentQuery.EndPackage);
                        }
                        else
                        {
                            ClearMessage();
                            ShowPackages(task.Result, _currentQuery.TotalItemCount, _currentQuery.BeginPackage, _currentQuery.EndPackage);
                        }

                        RestoreUI();
                    },
                    uiScheduler);
        }

        private IList<PackageInfo> QueryPackages(object state)
        {
            var token = (CancellationToken)state;

            IList<PackageInfo> result = _currentQuery.GetItemsForCurrentPage().ToList();

            token.ThrowIfCancellationRequested();

            IPackageRepository repository = GetPackageRepository(token);

            token.ThrowIfCancellationRequested();

            // this is the only way we can the download uri for each data service package
            var dataServiceRepository = repository as DataServicePackageRepository;
            if (dataServiceRepository != null)
            {
                foreach (PackageInfo entity in result)
                {
                    entity.DownloadUrl = dataServiceRepository.GetReadStreamUri(entity);
                }
            }

            token.ThrowIfCancellationRequested();
            return result;
        }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Maintainability", "CA1506:AvoidExcessiveClassCoupling"), System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Design", "CA1031:DoNotCatchGeneralExceptionTypes")]
        private void LoadPackages()
        {
            IsEditable = false;
            ShowMessage(Resources.ConnectingMessage, false);
            ClearPackages(isErrorCase: true);

            TaskScheduler uiScheduler = TaskScheduler.FromCurrentSynchronizationContext();

            CurrentCancellationTokenSource = new CancellationTokenSource();
            CancellationTokenSource usedTokenSource = CurrentCancellationTokenSource;

            Task.Factory.StartNew<IPackageRepository>(
                GetPackageRepository,
                CurrentCancellationTokenSource.Token,
                CurrentCancellationTokenSource.Token
                ).ContinueWith(
                    task =>
                    {
                        Exception exception = task.Exception;
                        if (usedTokenSource != CurrentCancellationTokenSource)
                        {
                            // this mean this request has already been canceled. No need to process this request anymore.
                            return;
                        }

                        if (task.IsFaulted)
                        {
                            ShowMessage(exception.GetBaseException().Message, isError: true);
                            RestoreUI();
                            return;
                        }
                        else if (task.IsCanceled)
                        {
                            ClearMessage();
                            RestoreUI();
                            return;
                        }

                        IPackageRepository repository = task.Result;
                        if (repository == null)
                        {
                            ClearMessage();
                            RestoreUI();
                            return;
                        }

                        IQueryable<IPackage> query = null;

                        if (!String.IsNullOrEmpty(_currentSearch))
                        {
                            var searchableRepository = repository as IPackageSearchable;
                            if (searchableRepository != null)
                            {
                                query = searchableRepository.Search(_currentSearch);
                            }
                        }

                        if (query == null)
                        {
                            try
                            {
                                query = repository.GetPackages();
                            }
                            catch (Exception error)
                            {
                                // only show error if user hasn't canceled this request
                                if (usedTokenSource == CurrentCancellationTokenSource)
                                {
                                    ShowMessage(error.GetBaseException().Message, isError: true);
                                    RestoreUI();
                                }
                                return;
                            }

                            if (!String.IsNullOrEmpty(_currentSearch))
                            {
                                query = query.Search(_currentSearch);
                            }
                        }

                        // When in Show All Versions mode, we can't sort by PackageSize. 
                        Debug.Assert(SortColumn != "PackageSize" || ShowLatestVersion);

                        switch (SortColumn)
                        {
                            case "Id":
                                query = SortDirection == ListSortDirection.Descending
                                            ? query.OrderByDescending(p => p.Id)
                                            : query.OrderBy(p => p.Id);
                                break;

                            case "VersionDownloadCount":
                                query = SortDirection == ListSortDirection.Descending
                                            ? query.OrderByDescending(p => p.DownloadCount)
                                            : query.OrderBy(p => p.DownloadCount);
                                break;

                            case "PackageSize":
                                query = SortDirection == ListSortDirection.Descending
                                            ? query.OrderByDescending(p => p.PackageSize)
                                            : query.OrderBy(p => p.PackageSize);
                                break;

                            default:
                                query = query.OrderByDescending(p => p.DownloadCount);
                                break;
                        }

                        if (ShowLatestVersion)
                        {
                            IQueryable<PackageInfo> packageInfos = GetPackageInfos(query, repository, getLatestVersions: true, showPrerelease: ShowPrereleasePackages);
                            _currentQuery = new ShowLatestVersionQueryContext<PackageInfo>(packageInfos, ShowLatestVersionPageSize);
                        }
                        else
                        {
                            /* show all versions */
                            IQueryable<PackageInfo> packageInfos = GetPackageInfos(query, repository, getLatestVersions: false, showPrerelease: ShowPrereleasePackages);
                            
                            _currentQuery = new ShowAllVersionsQueryContext<PackageInfo>(
                                packageInfos,
                                ShowAllVersionsPageSize,
                                PageBuffer,
                                PackageInfoEqualityComparer.Instance,
                                (a, b) => b.SemanticVersion.CompareTo(a.SemanticVersion));
                        }

                        LoadPage(CurrentCancellationTokenSource.Token);
                    },
                    CurrentCancellationTokenSource.Token,
                    TaskContinuationOptions.None,
                    uiScheduler
                );
        }

        private static IQueryable<PackageInfo> GetPackageInfos(
            IQueryable<IPackage> query,
            IPackageRepository repository,
            bool getLatestVersions,
            bool showPrerelease)
        {
            if (repository is DataServicePackageRepository)
            {
                if (getLatestVersions)
                {
                    if (repository.SupportsPrereleasePackages && showPrerelease)
                    {
                        query = query.Where(p => p.IsAbsoluteLatestVersion);
                    }
                    else
                    {
                        query = query.Where(p => p.IsLatestVersion);
                    }
                }
                else
                {
                    if (repository.SupportsPrereleasePackages) 
                    {
                        if (!showPrerelease)
                        {
                            query = query.Where(p => !p.IsPrerelease);
                        }
                    }
                }

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
                if (getLatestVersions)
                {
                    query = query.GroupBy(p => p.Id, StringComparer.OrdinalIgnoreCase)
                                 .Select(g => g.OrderByDescending(p => p.Version)
                                 .First());
                }

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

        private void Search(string searchTerm)
        {
            searchTerm = searchTerm ?? CurrentTypingSearch;
            searchTerm = searchTerm.Trim();
            if (_currentSearch != searchTerm)
            {
                _currentSearch = searchTerm;
                LoadPackages();
                CurrentTypingSearch = _currentSearch;
            }
        }

        private bool CanSearch(string searchTerm)
        {
            return IsEditable && !String.IsNullOrEmpty(searchTerm);
        }

        private void ClearSearch()
        {
            CurrentTypingSearch = _currentSearch = String.Empty;
            LoadPackages();
        }

        private bool CanClearSearch()
        {
            return IsEditable && !String.IsNullOrEmpty(_currentSearch);
        }

        private void Sort(string column)
        {
            if (column == "Version" || column == "Authors")
            {
                return;
            }
            Sort(column, null);
        }

        private bool CanSort(string column)
        {
            if (column == "PackageSize" && !ShowLatestVersion)
            {
                // We can't sort by PackageSize in ShowAllVersions mode
                return false;
            }

            return TotalPackageCount > 0;
        }

        private void Sort(string column, ListSortDirection? direction)
        {
            if (SortColumn == column)
            {
                if (direction.HasValue)
                {
                    SortDirection = direction.Value;
                }
                else
                {
                    SortDirection = SortDirection == ListSortDirection.Ascending
                                        ? ListSortDirection.Descending
                                        : ListSortDirection.Ascending;
                }
            }
            else
            {
                SortColumn = column;
                SortDirection = direction ?? ListSortDirection.Ascending;
            }

            LoadPackages();
        }

        private void ChangePackageSource(string source)
        {
            if (PackageSource != source)
            {
                // add the new source to MRU list
                _packageSourceManager.NotifyPackageSourceAdded(source);
                PackageSource = source;

                ResetPackageRepository();
                LoadPackages();
            }
            else
            {
                LoadPackages();
            }
        }

        private void UpdatePageNumber(int totalPackageCount, int beginPackage, int endPackage)
        {
            TotalPackageCount = totalPackageCount;
            BeginPackage = beginPackage;
            EndPackage = endPackage;
        }

        private void ClearPackages(bool isErrorCase)
        {
            Packages.Clear();
            if (isErrorCase)
            {
                UpdatePageNumber(0, 0, 0);
            }
        }

        private void ShowPackages(
            IEnumerable<PackageInfo> packages, int totalPackageCount, int beginPackage, int endPackage)
        {
            Packages.Clear();
            Packages.AddRange(packages);
            UpdatePageNumber(totalPackageCount, beginPackage, endPackage);
        }

        private void ShowMessage(string message, bool isError)
        {
            StatusContent = message;
            HasError = isError;
        }

        private void ClearMessage()
        {
            ShowMessage(String.Empty, isError: false);
        }

        public void OnAfterShow()
        {
            CurrentTypingSearch = _currentSearch;
        }

        private void RestoreUI()
        {
            IsEditable = true;
            CurrentCancellationTokenSource = null;
            LoadPackagesCompleted(this, EventArgs.Empty);
        }

        #region CancelCommand

        private void CancelCommandExecute()
        {
            if (CurrentCancellationTokenSource != null)
            {
                CurrentCancellationTokenSource.Cancel();
                ClearMessage();
                IsEditable = true;
            }
        }

        private bool CanCancelCommandExecute()
        {
            return !IsEditable && CurrentCancellationTokenSource != null;
        }

        #endregion

        #region NavigationCommand

        private bool NavigationCommandCanExecute(string action)
        {
            if (!IsEditable)
            {
                return false;
            }

            switch (action)
            {
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

        private void NavigationCommandExecute(string action)
        {
            switch (action)
            {
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

        private void MoveLast()
        {
            bool canMoveLast = _currentQuery.MoveLast();
            if (canMoveLast)
            {
                LoadPage(CancellationToken.None);
            }
        }

        private void MoveNext()
        {
            bool canMoveNext = _currentQuery.MoveNext();
            if (canMoveNext)
            {
                LoadPage(CancellationToken.None);
            }
        }

        private void MovePrevious()
        {
            bool canMovePrevious = _currentQuery.MovePrevious();
            if (canMovePrevious)
            {
                LoadPage(CancellationToken.None);
            }
        }

        private void MoveFirst()
        {
            _currentQuery.MoveFirst();
            LoadPage(CancellationToken.None);
        }

        private bool CanMoveLast()
        {
            return EndPackage < TotalPackageCount && ShowLatestVersion;
        }

        private bool CanMoveNext()
        {
            return EndPackage < TotalPackageCount;
        }

        private bool CanMovePrevious()
        {
            return BeginPackage > 1;
        }

        private bool CanMoveFirst()
        {
            return BeginPackage > 1;
        }

        #endregion
    }
}