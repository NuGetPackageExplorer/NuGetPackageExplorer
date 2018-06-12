using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Input;
using NuGet.Packaging;
using NuGet.Protocol;
using NuGet.Protocol.Core.Types;
using NuGetPe;

namespace PackageExplorerViewModel
{
    public sealed class PackageChooserViewModel : ViewModelBase, IDisposable
    {
        private const int PackageListPageSize = 15;
        
        private IQueryContext<IPackageSearchMetadata> _currentQuery;
        private string _currentSearch;
        private FeedType _feedType;
        private MruPackageSourceManager _packageSourceManager;
        private readonly string _defaultPackageSourceUrl;

        public PackageChooserViewModel(MruPackageSourceManager packageSourceManager,
                                       bool showPrereleasePackages,
                                       string defaultPackageSourceUrl)
        {
            _showPrereleasePackages = showPrereleasePackages;
            _defaultPackageSourceUrl = defaultPackageSourceUrl;
            Packages = new ObservableCollection<PackageInfoViewModel>();

            SearchCommand = new RelayCommand<string>(Search, CanSearch);
            ClearSearchCommand = new RelayCommand(ClearSearch, CanClearSearch);
            NavigationCommand = new RelayCommand<string>(NavigationCommandExecute, NavigationCommandCanExecute);
            LoadedCommand = new RelayCommand(async () => await LoadPackages());
            ChangePackageSourceCommand = new RelayCommand<string>(ChangePackageSource);
            CancelCommand = new RelayCommand(CancelCommandExecute, CanCancelCommandExecute);

            _packageSourceManager = packageSourceManager ?? throw new ArgumentNullException(nameof(packageSourceManager));
        }

        #region Bound Properties

        private string _currentTypingSearch;
        public string CurrentTypingSearch
        {
            get { return _currentTypingSearch; }
            set
            {
                if (_currentTypingSearch != value)
                {
                    _currentTypingSearch = value;
                    OnPropertyChanged();
                }
            }
        }

        private bool _showPrereleasePackages;
        public bool ShowPrereleasePackages
        {
            get { return _showPrereleasePackages; }
            set
            {
                if (_showPrereleasePackages != value)
                {
                    _showPrereleasePackages = value;
                    OnPropertyChanged();

                    OnShowPrereleasePackagesChange();
                }
            }
        }
        
        public string PackageSource
        {
            get { return _defaultPackageSourceUrl ?? _packageSourceManager.ActivePackageSource; }
            private set
            {
                if (_defaultPackageSourceUrl != null)
                {
                    throw new InvalidOperationException(
                        "Cannot set active package source when fixed package source is used.");
                }
                _packageSourceManager.ActivePackageSource = value;
                OnPropertyChanged();
            }
        }
        
        public bool AllowsChangingPackageSource
        {
            get { return _defaultPackageSourceUrl == null; }
        }

        public ObservableCollection<string> PackageSources
        {
            get { return _packageSourceManager.PackageSources; }
        }

        private bool _isEditable = true;
        public bool IsEditable
        {
            get { return _isEditable; }
            set
            {
                if (_isEditable != value)
                {
                    _isEditable = value;
                    OnPropertyChanged();
                    NavigationCommand.RaiseCanExecuteChanged();
                }
            }
        }
        
        public ObservableCollection<PackageInfoViewModel> Packages { get; private set; }
        
        private PackageInfoViewModel _selectedPackageViewModel;
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
        
        private int _beginPackage;
        public int BeginPackage
        {
            get { return _beginPackage; }
            private set
            {
                if (_beginPackage != value)
                {
                    _beginPackage = value;
                    OnPropertyChanged();
                }
            }
        }

        private int _endPackage;
        public int EndPackage
        {
            get { return _endPackage; }
            private set
            {
                if (_endPackage != value)
                {
                    _endPackage = value;
                    OnPropertyChanged();
                }
            }
        }

        private string _statusContent;
        public string StatusContent
        {
            get { return _statusContent; }
            set
            {
                if (_statusContent != value)
                {
                    _statusContent = value;
                    OnPropertyChanged();
                }
            }
        }

        private bool _hasError;
        public bool HasError
        {
            get { return _hasError; }
            set
            {
                if (_hasError != value)
                {
                    _hasError = value;
                    OnPropertyChanged();
                }
            }
        }
        
        #endregion

        public SourceRepository ActiveRepository { get; private set; }

        public PackageInfo SelectedPackage
        {
            get
            {
                return _selectedPackageViewModel?.SelectedPackage;
            }
        }
        
        private CancellationTokenSource _currentCancellationTokenSource;
        private CancellationTokenSource CurrentCancellationTokenSource
        {
            get { return _currentCancellationTokenSource; }
            set
            {
                _currentCancellationTokenSource = value;
                CancelCommand.RaiseCanExecuteChanged();
            }
        }
        
        #region Commands
        public RelayCommand<string> NavigationCommand { get; private set; }
        public ICommand SearchCommand { get; private set; }
        public ICommand ClearSearchCommand { get; private set; }
        public ICommand LoadedCommand { get; private set; }
        public ICommand ChangePackageSourceCommand { get; private set; }
        public RelayCommand CancelCommand { get; private set; }
        #endregion

        #region EventHandler
        public event EventHandler LoadPackagesCompleted = delegate { };
        public event EventHandler OpenPackageRequested = delegate { };
        public event EventHandler PackageDownloadRequested = delegate { };
        #endregion

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Maintainability", "CA1506:AvoidExcessiveClassCoupling"), 
         System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Design", "CA1031:DoNotCatchGeneralExceptionTypes")]
        private async Task LoadPackages()
        {
            IsEditable = false;
            ClearPackages(isErrorCase: true);

            CurrentCancellationTokenSource = new CancellationTokenSource();
            var usedTokenSource = CurrentCancellationTokenSource;

            var repository = GetPackageRepository();
            if (repository == null)
            {
                await LoadPage(CurrentCancellationTokenSource.Token);
                return;
            }

            _currentQuery = new ShowLatestVersionQueryContext<IPackageSearchMetadata>(repository, _currentSearch, ShowPrereleasePackages, PackageListPageSize);
            _feedType = await repository.GetFeedType(usedTokenSource.Token);

            await LoadPage(CurrentCancellationTokenSource.Token);
        }

        private void ClearPackages(bool isErrorCase)
        {
            Packages.Clear();
            if (isErrorCase)
            {
                UpdatePageNumber(0, 0);
            }
        }

        private SourceRepository GetPackageRepository()
        {
            if (ActiveRepository == null)
            {
                _feedType = FeedType.Undefined;
                ActiveRepository = PackageRepositoryFactory.CreateRepository(PackageSource);
            }

            return ActiveRepository;
        }

        private async Task LoadPage(CancellationToken token)
        {
            Debug.Assert(_currentQuery != null);

            IsEditable = false;
            ClearPackages(isErrorCase: false);

            if (token == CancellationToken.None)
            {
                CurrentCancellationTokenSource = new CancellationTokenSource();
                token = CurrentCancellationTokenSource.Token;
            }

            var usedTokenSource = CurrentCancellationTokenSource;

            try
            {
                var packageInfos = await QueryPackages(token);

                if (usedTokenSource != CurrentCancellationTokenSource)
                {
                    // This request has already been canceled. No need to process this request anymore.
                    return;
                }

                ClearMessage();
                ShowPackages(packageInfos, _currentQuery.BeginPackage, _currentQuery.EndPackage);
            }
            catch (OperationCanceledException)
            {
                if (usedTokenSource != CurrentCancellationTokenSource)
                {
                    // This request has already been canceled. No need to process this request anymore.
                    return;
                }

                ClearMessage();
                UpdatePageNumber(_currentQuery.BeginPackage, _currentQuery.EndPackage);
            }
            catch (Exception exception)
            {
                if (usedTokenSource != CurrentCancellationTokenSource)
                {
                    // This request has already been canceled. No need to process this request anymore.
                    return;
                }

                var errorMessage = exception.Message;

                ShowMessage(errorMessage, true);
                ClearPackages(isErrorCase: true);
            }

            AutoSelectFirstAvailablePackage();
            RestoreUI();
        }
        
        private async Task<IList<IPackageSearchMetadata>> QueryPackages(CancellationToken token)
        {
            var result = await _currentQuery.GetItemsForCurrentPage(token);
            token.ThrowIfCancellationRequested();
            return result;
        }

        private void ShowPackages(IEnumerable<IPackageSearchMetadata> packages, int beginPackage, int endPackage)
        {
            Packages.Clear();
            if (ActiveRepository != null)
            {
                Packages.AddRange(packages.Select(p => new PackageInfoViewModel(p, ShowPrereleasePackages, ActiveRepository, _feedType, this)));
            }
            UpdatePageNumber(beginPackage, endPackage);
        }
        
        private void UpdatePageNumber(int beginPackage, int endPackage)
        {
            BeginPackage = beginPackage;
            EndPackage = endPackage;
        }
        
        private void AutoSelectFirstAvailablePackage()
        {
            SelectedPackageViewModel = Packages?.FirstOrDefault();
        }

        private void RestoreUI()
        {
            IsEditable = true;
            CurrentCancellationTokenSource = null;
            LoadPackagesCompleted(this, EventArgs.Empty);
        }
        
        #region Search
        private async void Search(string searchTerm)
        {
            searchTerm = searchTerm ?? CurrentTypingSearch ?? string.Empty;
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
            return IsEditable && !string.IsNullOrEmpty(searchTerm);
        }

        private async void ClearSearch()
        {
            CurrentTypingSearch = _currentSearch = string.Empty;
            await LoadPackages();
        }

        private bool CanClearSearch()
        {
            return IsEditable && !string.IsNullOrEmpty(_currentSearch);
        }
        #endregion

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
        
        private void ResetPackageRepository()
        {
            ActiveRepository = null;
        }

        #region Status Bar
        private void ShowMessage(string message, bool isError)
        {
            StatusContent = message;
            HasError = isError;
        }

        private void ClearMessage()
        {
            ShowMessage(string.Empty, isError: false);
        }
        #endregion

        private async void OnShowPrereleasePackagesChange()
        {
            await LoadPackages();
        }

        public void OnAfterShow()
        {
            CurrentTypingSearch = _currentSearch;
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
            }
        }

        private Task MoveNext()
        {
            var canMoveNext = _currentQuery.MoveNext();
            if (canMoveNext)
            {
                return LoadPage(CancellationToken.None);
            }

            return Task.FromResult(0);
        }

        private Task MovePrevious()
        {
            var canMovePrevious = _currentQuery.MovePrevious();
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

        private bool CanMoveNext()
        {
            return !_currentQuery?.IsLastPage ?? false;
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

            CurrentCancellationTokenSource?.Dispose();
        }
    }
}
