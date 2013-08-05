using System;
using System.ComponentModel;
using System.Diagnostics;
using System.Threading;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Threading;
using NuGet;
using PackageExplorer.Properties;
using PackageExplorerViewModel;

namespace PackageExplorer
{
    /// <summary>
    /// Interaction logic for PackageChooserDialog.xaml
    /// </summary>
    public partial class PackageChooserDialog : StandardDialog
    {
        private readonly PackageChooserViewModel _viewModel;
        private string _pendingSearch;        

        public PackageChooserDialog(PackageChooserViewModel viewModel)
        {
            InitializeComponent();

            Debug.Assert(viewModel != null);

            _viewModel = viewModel;
            _viewModel.LoadPackagesCompleted += OnLoadPackagesCompleted;
            _viewModel.PropertyChanged += OnViewModelPropertyChanged;

            DataContext = _viewModel;
        }

        public PackageInfo SelectedPackage
        {
            get 
            { 
                return PackageGrid.SelectedItem as PackageInfo; 
            }
        }

        private void OnViewModelPropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == "SortDirection")
            {
                RedrawSortGlyph(_viewModel.SortColumn, _viewModel.SortDirection);
            }
        }

        private void OnLoadPackagesCompleted(object sender, EventArgs e)
        {
            // Ensure that the SearchBox is focused after the packages have loaded so that the user can search right
            // away if they need to. Currently the default search behavior is not working most likely do to the
            // controls being disabled when the packages are loading.
            FocusSearchBox();
        }

        private void RedrawSortGlyph(string sortColumn, ListSortDirection sortDirection)
        {
            foreach (DataGridColumn column in PackageGrid.Columns)
            {
                if (column.SortMemberPath.Equals(sortColumn, StringComparison.OrdinalIgnoreCase))
                {
                    column.SortDirection = _viewModel.SortDirection;
                    break;
                }
            }
        }

        private void OkButton_Click(object sender, RoutedEventArgs e)
        {
            Hide();
        }

        private void CloseButton_Click(object sender, RoutedEventArgs e)
        {
            CancelPendingRequestAndCloseDialog();
        }

        private void CancelPendingRequestAndCloseDialog()
        {
            CancelPendingRequest();
            PackageGrid.SelectedItem = null;
            Hide();
        }

        private void CancelPendingRequest()
        {
            _viewModel.CancelCommand.Execute(null);
        }

        private void SearchBox_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
            {
                InvokeSearch(null);
                e.Handled = true;
            }
            else if (e.Key == Key.Escape)
            {
                ICommand clearSearchCommand = ClearSearchButton.Command;
                if (clearSearchCommand.CanExecute(null))
                {
                    // simulate Clear Search command execution
                    clearSearchCommand.Execute(null);
                    e.Handled = true;
                }
            }
        }

        private void InvokeSearch(string searchTerm)
        {
            // simulate Search command execution
            SearchButton.Command.Execute(searchTerm);
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            AdjustSearchBox();

            if (String.IsNullOrEmpty(_pendingSearch))
            {
                Dispatcher.BeginInvoke(new Action(LoadPackages), DispatcherPriority.Background);
            }
            else
            {
                Dispatcher.BeginInvoke(
                    new Action<string>(InvokeSearch),
                    DispatcherPriority.Background,
                    _pendingSearch);
            }
        }

        private void AdjustSearchBox()
        {
            // HACK: Make space for the search image inside the search box
            if (SearchBox.Template != null)
            {
                var contentHost = SearchBox.Template.FindName("PART_ContentHost", SearchBox) as FrameworkElement;
                if (contentHost != null)
                {
                    contentHost.Margin = new Thickness(0, 0, 40, 0);
                    contentHost.Width = 160;
                }
            }
        }

        private void LoadPackages()
        {
            var loadedCommand = (ICommand) Tag;
            loadedCommand.Execute(null);
        }

        private void Window_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.E && Keyboard.Modifiers == ModifierKeys.Control)
            {
                FocusSearchBox();
                e.Handled = true;
            }
        }

        private void OnWindowSizeChanged(object sender, SizeChangedEventArgs e)
        {
            if (!e.NewSize.IsEmpty)
            {
                Settings settings = Settings.Default;
                settings.PackageChooserDialogHeight = e.NewSize.Height;
                settings.PackageChooserDialogWidth = e.NewSize.Width;
            }
        }

        private void StandardDialog_Closing(object sender, CancelEventArgs e)
        {
            e.Cancel = true;
            CancelPendingRequestAndCloseDialog();
        }

        internal void ForceClose()
        {
            Closing -= StandardDialog_Closing;
            Close();
        }

        private void FocusSearchBox()
        {
            bool gotFocus = SearchBox.Focus();
            if (gotFocus)
            {
                // move caret to the end 
                SearchBox.Select(SearchBox.Text.Length, 0);
            }
        }

        private void OnAfterShow()
        {
            _viewModel.OnAfterShow();
            FocusSearchBox();
        }

        private void PackageSourceBox_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
            {
                string source = PackageSourceBox.Text;
                if (!String.IsNullOrEmpty(source))
                {
                    _viewModel.ChangePackageSourceCommand.Execute(source);
                    e.Handled = true;
                }
            }
        }

        internal void ShowDialog(string searchTerm)
        {
            _pendingSearch = searchTerm;
            ShowDialog();
            _pendingSearch = null;
        }

        private void StandardDialog_IsVisibleChanged(object sender, DependencyPropertyChangedEventArgs e)
        {
            // The first time this event handler is invoked, IsLoaded = false
            // We only do work from the second time.
            if (IsVisible && IsLoaded)
            {
                if (String.IsNullOrEmpty(_pendingSearch))
                {
                    // there is no pending search operation, just set focus on the search box
                    Dispatcher.BeginInvoke(new Action(OnAfterShow), DispatcherPriority.Background);
                }
                else
                {
                    InvokeSearch(_pendingSearch);
                }
            }
        }

        private void PackageGrid_Sorting(object sender, DataGridSortingEventArgs e)
        {
            if (_viewModel.SortCommand.CanExecute(e.Column.SortMemberPath))
            {
                _viewModel.SortCommand.Execute(e.Column.SortMemberPath);
            }

            e.Handled = true;
        }

        private void OnPackageRowDetailsLoaded(object sender, RoutedEventArgs e)
        {
            // align the nested ListView's columns with the parent DataGrid's columns
            var packageRowDetails = (PackageRowDetails)sender;
            packageRowDetails.ApplyBindings(PackageGrid);
        }
    }
}