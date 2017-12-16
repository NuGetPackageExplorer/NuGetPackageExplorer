using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Data.Services.Client;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Input;
using NuGet.Packaging;
using NuGetPe;
using PackageExplorerViewModel.Types;

namespace PackageExplorerViewModel
{
    public sealed class PackageChooserViewModel : ViewModelBase, IDisposable
    {
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
        private readonly ICredentialManager _credentialManager;
        private bool _showPrereleasePackages;
        private bool _autoLoadPackages;
        private string _sortColumn;
        private ListSortDirection _sortDirection;
        private string _statusContent;
        private int _totalPackageCount;
        private PackageInfoViewModel _selectedPackageViewModel;

        public PackageChooserViewModel(
            MruPackageSourceManager packageSourceManager,
            ICredentialManager credentialManager,
            bool showPrereleasePackages,
            bool autoLoadPackages,
            string fixedPackageSource)
        {
            _showPrereleasePackages = showPrereleasePackages;
            _fixedPackageSource = fixedPackageSource;
            _autoLoadPackages = autoLoadPackages;
            Packages = new ObservableCollection<PackageInfoViewModel>();
            SortCommand = new RelayCommand<string>(Sort, CanSort);
            SearchCommand = new RelayCommand<string>(Search, CanSearch);
            ClearSearchCommand = new RelayCommand(ClearSearch, CanClearSearch);
            NavigationCommand = new RelayCommand<string>(NavigationCommandExecute, NavigationCommandCanExecute);
            LoadedCommand = new RelayCommand(async () => await Sort("VersionDownloadCount", ListSortDirection.Descending));
            ChangePackageSourceCommand = new RelayCommand<string>(ChangePackageSource);
            CancelCommand = new RelayCommand(CancelCommandExecute, CanCancelCommandExecute);
            _packageSourceManager = packageSourceManager ?? throw new ArgumentNullException("packageSourceManager");
            _credentialManager = credentialManager ?? throw new ArgumentNullException("credentialManager");
        }

        public IPackageRepository ActiveRepository
        {
            get
            {
                return _packageRepository;
            }
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

        public PackageInfoViewModel SelectedPackageViewModel
        {
            get { return _selectedPackageViewModel; }
            set
            {
                if (_selectedPackageViewModel != value)
                {
                    if (_selectedPackageViewModel != null)
                    {
                        _selectedPackageViewModel.OnDeselected();
                    }

                    _selectedPackageViewModel = value;
                    OnPropertyChanged();
                }
            }
        }

        public PackageInfo SelectedPackage
        {
            get
            {
                return _selectedPackageViewModel == null ? null : _selectedPackageViewModel.EffectiveSelectedPackage;
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

        public bool AutoLoadPackages
        {
            get { return _autoLoadPackages; }
            set
            {
                if (_autoLoadPackages != value)
                {
                    _autoLoadPackages = value;
                    OnPropertyChanged();
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

        public ObservableCollection<PackageInfoViewModel> Packages { get; private set; }

        public RelayCommand<string> NavigationCommand { get; private set; }
        public ICommand SortCommand { get; private set; }
        public ICommand SearchCommand { get; private set; }
        public ICommand ClearSearchCommand { get; private set; }
        public ICommand LoadedCommand { get; private set; }
        public ICommand ChangePackageSourceCommand { get; private set; }
        public RelayCommand CancelCommand { get; private set; }

        public event EventHandler LoadPackagesCompleted = delegate { };
        public event EventHandler OpenPackageRequested = delegate { };
        public event EventHandler PackageDownloadRequested = delegate { };

        private async void OnShowPrereleasePackagesChange()
        {
            await Sort(SortColumn, SortDirection);
        }

        private IPackageRepository GetPackageRepository()
        {
            if (_packageRepository == null)
            {
                _packageRepository = PackageRepositoryFactory.CreateRepository(PackageSource, _credentialManager);
            }

            return _packageRepository;
        }

        private void ResetPackageRepository()
        {
            _packageRepository = null;
        }

        internal async Task LoadPage(CancellationToken token)
        {
            Debug.Assert(_currentQuery != null);

            IsEditable = false;
            //ShowMessage(Resources.LoadingMessage, false);
            ClearPackages(isErrorCase: false);

            if (token == CancellationToken.None)
            {
                CurrentCancellationTokenSource = new CancellationTokenSource();
                token = CurrentCancellationTokenSource.Token;
            }

            CancellationTokenSource usedTokenSource = CurrentCancellationTokenSource;

            try
            {
                IList<PackageInfo> packageInfos = await QueryPackages(token);

                if (usedTokenSource != CurrentCancellationTokenSource)
                {
                    // this mean this request has already been canceled. No need to process this request anymore.
                    return;
                }

                ClearMessage();
                ShowPackages(packageInfos, _currentQuery.TotalItemCount, _currentQuery.BeginPackage, _currentQuery.EndPackage);
            }
            catch (OperationCanceledException)
            {
                if (usedTokenSource != CurrentCancellationTokenSource)
                {
                    // this mean this request has already been canceled. No need to process this request anymore.
                    return;
                }

                ClearMessage();
                UpdatePageNumber(_currentQuery.TotalItemCount, _currentQuery.BeginPackage, _currentQuery.EndPackage);
            }
            catch (Exception exception)
            {
                if (usedTokenSource != CurrentCancellationTokenSource)
                {
                    // this mean this request has already been canceled. No need to process this request anymore.
                    return;
                }

                string errorMessage = exception.Message;

                var queryException = exception as DataServiceQueryException;
                if (queryException != null && queryException.Response != null)
                {
                    errorMessage = errorMessage + ". The remote server returned status code: " + queryException.Response.StatusCode + ".";
                }

                ShowMessage(errorMessage, true);
                ClearPackages(isErrorCase: true);
            }

            RestoreUI();
        }

        private async Task<IList<PackageInfo>> QueryPackages(CancellationToken token)
        {
            IList<PackageInfo> result = await _currentQuery.GetItemsForCurrentPage(token);

            IPackageRepository repository = GetPackageRepository();

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
        private Task LoadPackages()
        {
            IsEditable = false;
            ClearPackages(isErrorCase: true);

            CurrentCancellationTokenSource = new CancellationTokenSource();
            CancellationTokenSource usedTokenSource = CurrentCancellationTokenSource;

            IPackageRepository repository = GetPackageRepository();

            if (repository == null)
            {
                return LoadPage(CurrentCancellationTokenSource.Token);
            }

            IQueryable<IPackage> query = null;

            // special case for searching
            if (!String.IsNullOrEmpty(_currentSearch))
            {
                var searchableRepository = repository as IPackageSearchable;
                if (searchableRepository != null)
                {
                    query = searchableRepository.Search(_currentSearch, ShowPrereleasePackages);
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
                        ShowMessage(error.Message, isError: true);
                        RestoreUI();
                    }
                    return Task.FromResult(0);
                }

                if (!String.IsNullOrEmpty(_currentSearch))
                {
                    query = query.Search(_currentSearch);
                }
            }

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

                case "Published":
                    query = SortDirection == ListSortDirection.Descending
                                ? query.OrderByDescending(p => p.Published)
                                : query.OrderBy(p => p.Published);
                    break;

                default:
                    query = query.OrderByDescending(p => p.DownloadCount);
                    break;
            }

            IQueryable<PackageInfo> packageInfos = GetPackageInfos(query, repository, showPrerelease: ShowPrereleasePackages);
            _currentQuery = new ShowLatestVersionQueryContext<PackageInfo>(packageInfos, ShowLatestVersionPageSize);

            return LoadPage(CurrentCancellationTokenSource.Token);
        }

        private static IQueryable<PackageInfo> GetPackageInfos(IQueryable<IPackage> query, IPackageRepository repository, bool showPrerelease)
        {
            if (repository is DataServicePackageRepository)
            {
                if (showPrerelease)
                {
                    query = query.Where(p => p.IsAbsoluteLatestVersion);
                }
                else
                {
                    query = query.Where(p => p.IsLatestVersion);
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
                query = query.GroupBy(p => p.Id, StringComparer.OrdinalIgnoreCase)
                             .Select(g => g.OrderByDescending(p => p.Version)
                             .First());

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

        private async void Search(string searchTerm)
        {
            searchTerm = searchTerm ?? CurrentTypingSearch ?? String.Empty;
            searchTerm = searchTerm.Trim();
            if (_currentSearch != searchTerm)
            {
                _currentSearch = searchTerm;
                await LoadPackages();
                CurrentTypingSearch = _currentSearch;
            }
        }

        private bool CanSearch(string searchTerm)
        {
            return IsEditable && !String.IsNullOrEmpty(searchTerm);
        }

        private async void ClearSearch()
        {
            CurrentTypingSearch = _currentSearch = String.Empty;
            await LoadPackages();
        }

        private bool CanClearSearch()
        {
            return IsEditable && !String.IsNullOrEmpty(_currentSearch);
        }

        private async void Sort(string column)
        {
            if (column == "Version" || column == "Authors")
            {
                return;
            }

            await Sort(column, null);
        }

        private bool CanSort(string column)
        {
            return TotalPackageCount > 0;
        }

        private Task Sort(string column, ListSortDirection? direction)
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

            return LoadPackages();
        }

        private async void ChangePackageSource(string source)
        {
            if (PackageSource != source)
            {
                // add the new source to MRU list
                _packageSourceManager.NotifyPackageSourceAdded(source);
                PackageSource = source;

                ResetPackageRepository();
                await LoadPackages();
            }
            else
            {
                await LoadPackages();
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
            if (_packageRepository != null)
            {
                Packages.AddRange(packages.Select(p => new PackageInfoViewModel(p, ShowPrereleasePackages, _packageRepository, this)));
            }
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

        internal void OnOpenPackage()
        {
            OpenPackageRequested(this, EventArgs.Empty);
        }

        internal void OnDownloadPackage()
        {
            PackageDownloadRequested(this, EventArgs.Empty);
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

        private async void NavigationCommandExecute(string action)
        {
            switch (action)
            {
                case "First":
                    await MoveFirst();
                    break;

                case "Previous":
                    await MovePrevious();
                    break;

                case "Next":
                    await MoveNext();
                    break;

                case "Last":
                    await MoveLast();
                    break;
            }
        }

        private Task MoveLast()
        {
            bool canMoveLast = _currentQuery.MoveLast();
            if (canMoveLast)
            {
                return LoadPage(CancellationToken.None);
            }

            return Task.FromResult(0);
        }

        private Task MoveNext()
        {
            bool canMoveNext = _currentQuery.MoveNext();
            if (canMoveNext)
            {
                return LoadPage(CancellationToken.None);
            }

            return Task.FromResult(0);
        }

        private Task MovePrevious()
        {
            bool canMovePrevious = _currentQuery.MovePrevious();
            if (canMovePrevious)
            {
                return LoadPage(CancellationToken.None);
            }

            return Task.FromResult(0);
        }

        private Task MoveFirst()
        {
            _currentQuery.MoveFirst();
            return LoadPage(CancellationToken.None);
        }

        private bool CanMoveLast()
        {
            return EndPackage < TotalPackageCount;
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
    }
}