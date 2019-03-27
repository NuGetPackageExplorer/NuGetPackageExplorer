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
using NuGetPackageExplorer.Types;
using NuGetPe;

namespace PackageExplorerViewModel
{
    public sealed class PackageChooserViewModel : ViewModelBase, IDisposable
    {
        private const int PackageListPageSize = 15;

        private IQueryContext<IPackageSearchMetadata>? _currentQuery;
        private string? _currentSearch;
        private FeedType _feedType;
        private MruPackageSourceManager? _packageSourceManager;
        private readonly IUIServices _uIServices;
        private readonly string? _defaultPackageSourceUrl;
        private bool _disposed;

        public PackageChooserViewModel(MruPackageSourceManager packageSourceManager,
                                       IUIServices uIServices,
                                       bool showPrereleasePackages,
                                       string? defaultPackageSourceUrl)
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
            _uIServices = uIServices;
        }

        #region Bound Properties

        private string? _currentTypingSearch;
        public string? CurrentTypingSearch
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
            get
            {
                CheckDisposed();
                return _defaultPackageSourceUrl ?? _packageSourceManager!.ActivePackageSource;
            }
            private set
            {
                if (_defaultPackageSourceUrl != null)
                {
                    throw new InvalidOperationException(
                        "Cannot set active package source when fixed package source is used.");
                }
                CheckDisposed();
                _packageSourceManager!.ActivePackageSource = value.Trim();
                OnPropertyChanged();
            }
        }

        public bool AllowsChangingPackageSource
        {
            get { return _defaultPackageSourceUrl == null; }
        }

        public ObservableCollection<string> PackageSources
        {
            get
            {
                CheckDisposed();
                return _packageSourceManager!.PackageSources;
            }
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

        private PackageInfoViewModel? _selectedPackageViewModel;
        public PackageInfoViewModel? SelectedPackageViewModel
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

        private string? _statusContent;
        public string? StatusContent
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

        public SourceRepository? ActiveRepository { get; private set; }

        public PackageInfo? SelectedPackage
        {
            get
            {
                return _selectedPackageViewModel?.SelectedPackage;
            }
        }

        private CancellationTokenSource? _currentCancellationTokenSource;
        private CancellationTokenSource? CurrentCancellationTokenSource
        {
            get { return _currentCancellationTokenSource; }
            set
            {
                _currentCancellationTokenSource = value;
                CancelCommand.RaiseCanExecuteChanged();
            }
        }

        public RelayCommand<string> NavigationCommand { get; private set; }
        public ICommand SearchCommand { get; private set; }
        public ICommand ClearSearchCommand { get; private set; }
        public ICommand LoadedCommand { get; private set; }
        public ICommand ChangePackageSourceCommand { get; private set; }
        public RelayCommand CancelCommand { get; private set; }

        public event EventHandler LoadPackagesCompleted = delegate { };
        public event EventHandler OpenPackageRequested = delegate { };
        public event EventHandler PackageDownloadRequested = delegate { };


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
                try
                {
                    ActiveRepository = PackageRepositoryFactory.CreateRepository(PackageSource);
                }
                catch (ArgumentException)
                {
                    var origSource = PackageSource;
                    PackageSource = _defaultPackageSourceUrl ?? NuGetConstants.DefaultFeedUrl;
                    ActiveRepository = PackageRepositoryFactory.CreateRepository(PackageSource);

                    _uIServices.Show($"Package Source '{origSource}' is not valid. Defaulting to '{NuGetConstants.DefaultFeedUrl}", MessageLevel.Error);
                }
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
            if (_currentQuery != null)
            {
                var result = await _currentQuery.GetItemsForCurrentPage(token);
                token.ThrowIfCancellationRequested();
                return result;
            }
            return new List<IPackageSearchMetadata>();
        }

        private void ShowPackages(IEnumerable<IPackageSearchMetadata> packages, int beginPackage, int endPackage)
        {
            Packages.Clear();
            if (ActiveRepository != null)
            {
                var ar = ActiveRepository;
                Packages.AddRange(packages.Select(p => new PackageInfoViewModel(p, ShowPrereleasePackages, ar, _feedType, this)));
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
            searchTerm ??= CurrentTypingSearch ?? string.Empty;
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
                DiagnosticsClient.TrackEvent("PackageChooserViewModel_ChangePackageSource");

                CheckDisposed();


                PackageSource = source;

                ResetPackageRepository();
                try
                {
                    await LoadPackages();

                    // add the new source to MRU list, after the load succeeds, in case there's an error with the source
                    _packageSourceManager!.NotifyPackageSourceAdded(source);
                }
                catch (Exception e)
                {
                    _uIServices.Show(e.Message, MessageLevel.Error);
                }
            }
            else
            {
                try
                {
                    await LoadPackages();
                }
                catch (Exception e)
                {
                    _uIServices.Show(e.Message, MessageLevel.Error);
                }
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
            DiagnosticsClient.TrackEvent("PackageChooserViewModel_OnShowPrereleasePackagesChange");

            await LoadPackages();
        }

        public void OnAfterShow()
        {
            CurrentTypingSearch = _currentSearch;
        }

        internal void OnOpenPackage()
        {
            DiagnosticsClient.TrackEvent("PackageChooserViewModel_OnOpenPackage");
            OpenPackageRequested(this, EventArgs.Empty);
        }

        internal void OnDownloadPackage()
        {
            DiagnosticsClient.TrackEvent("PackageChooserViewModel_OnDownloadPackage");
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
            DiagnosticsClient.TrackEvent($"NavigationCommandExecute_{action}");

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
            var canMoveNext = _currentQuery?.MoveNext();
            if (canMoveNext == true)
            {
                return LoadPage(CancellationToken.None);
            }

            return Task.FromResult(0);
        }

        private Task MovePrevious()
        {
            var canMovePrevious = _currentQuery?.MovePrevious();
            if (canMovePrevious == true)
            {
                return LoadPage(CancellationToken.None);
            }

            return Task.FromResult(0);
        }

        private Task MoveFirst()
        {
            _currentQuery?.MoveFirst();
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

            _disposed = true;
            CurrentCancellationTokenSource?.Dispose();
        }

        private void CheckDisposed()
        {
            if (_disposed) throw new ObjectDisposedException(nameof(PackageChooserViewModel));
        }
    }
}
