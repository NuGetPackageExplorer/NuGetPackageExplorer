using System;
using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Threading;
using NuGet;
using PackageExplorerViewModel;

namespace PackageExplorer {
    /// <summary>
    /// Interaction logic for PackageChooserDialog.xaml
    /// </summary>
    public partial class PackageChooserDialog : StandardDialog {

        public string SortColumn {
            get { return (string)GetValue(SortColumnProperty); }
            set { SetValue(SortColumnProperty, value); }
        }

        // Using a DependencyProperty as the backing store for SortColumn.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty SortColumnProperty =
            DependencyProperty.Register("SortColumn", typeof(string), typeof(PackageChooserDialog), null);

        public ListSortDirection SortDirection {
            get { return (ListSortDirection)GetValue(SortDirectionProperty); }
            set { SetValue(SortDirectionProperty, value); }
        }

        // Using a DependencyProperty as the backing store for SortDirection.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty SortDirectionProperty =
            DependencyProperty.Register("SortDirection", typeof(ListSortDirection), typeof(PackageChooserDialog), null);

        private static void OnSortCounterPropertyChanged(object sender, DependencyPropertyChangedEventArgs args) {
            var dialog = (PackageChooserDialog)sender;
            dialog.RedrawSortGlyph();
        }

        public PackageChooserDialog(PackageChooserViewModel viewModel) {
            InitializeComponent();

            SetBinding(SortColumnProperty, new Binding("SortColumn") { Mode = BindingMode.OneWay });
            SetBinding(SortDirectionProperty, new Binding("SortDirection") { Mode = BindingMode.OneWay });

            viewModel.LoadPackagesCompleted += new EventHandler(OnLoadPackagesCompleted);

            DataContext = viewModel;
        }

        private void OnLoadPackagesCompleted(object sender, EventArgs e) {
            if (_collectionDeferRefresh != null) {
                _collectionDeferRefresh.Dispose();
            }

            // Ensure that the SearchBox is focused after the packages have loaded so that the user can search right
            // away if they need to. Currently the default search behavior is not working most likely do to the
            // controls being disabled when the packages are loading.
            SearchBox.Focus();

            RedrawSortGlyph();
        }

        private void RedrawSortGlyph() {
            foreach (var column in PackageGridView.Columns) {
                var header = (GridViewColumnHeader)column.Header;
                if (header.Tag != null) {
                    AdornerLayer layer = AdornerLayer.GetAdornerLayer(header);
                    if (layer != null) {
                        layer.Remove((Adorner)header.Tag);
                    }
                }

                if ((string)header.CommandParameter == SortColumn) {
                    var newAdorner = new SortAdorner(header, SortDirection);
                    header.Tag = newAdorner;

                    AdornerLayer layer = AdornerLayer.GetAdornerLayer(header);
                    if (layer != null) {
                        layer.Add(newAdorner);
                    }
                }
            }
        }

        public PackageInfo SelectedPackage {
            get {
                return PackageGrid.SelectedItem as PackageInfo;
            }
        }

        private void CancelButton_Click(object sender, RoutedEventArgs e) {
            PackageGrid.SelectedItem = null;
            Hide();
        }

        private void OkButton_Click(object sender, RoutedEventArgs e) {
            Hide();
        }

        private void SearchBox_KeyDown(object sender, System.Windows.Input.KeyEventArgs e) {
            string searchTerm = null;

            if (e.Key == Key.Enter) {
                searchTerm = SearchBox.Text;
            }
            else if (e.Key == Key.Escape) {
                if (!String.IsNullOrEmpty(SearchBox.Text)) {
                    searchTerm = String.Empty;
                    SearchBox.Text = String.Empty;
                }
            }

            if (searchTerm != null) {
                Search(searchTerm);
                e.Handled = true;
            }
        }

        private void Search(string searchTerm) {
            ICommand searchCommand = (ICommand)SearchBox.Tag;
            searchCommand.Execute(searchTerm);
        }

        private void Window_Loaded(object sender, RoutedEventArgs e) {
            AdjustSearchBox();

            Dispatcher.BeginInvoke(new Action(LoadPackages), DispatcherPriority.Background);
        }

        private void AdjustSearchBox() {
            // HACK: Make space for the search image inside the search box
            if (SearchBox.Template != null) {
                var contentHost = SearchBox.Template.FindName("PART_ContentHost", SearchBox) as FrameworkElement;
                if (contentHost != null) {
                    contentHost.Margin = new Thickness(0, 0, 20, 0);
                    contentHost.Width = 150;
                }
            }
        }

        private void LoadPackages() {
            var loadedCommand = (ICommand)Tag;
            loadedCommand.Execute(null);
        }

        private void Window_PreviewKeyDown(object sender, KeyEventArgs e) {
            if (e.Key == Key.E && Keyboard.Modifiers == ModifierKeys.Control) {
                SearchBox.Focus();
                e.Handled = true;
            }
        }

        private void OnWindowSizeChanged(object sender, SizeChangedEventArgs e) {
            if (!e.NewSize.IsEmpty) {
                var settings = Properties.Settings.Default;
                settings.PackageChooserDialogHeight = e.NewSize.Height;
                settings.PackageChooserDialogWidth = e.NewSize.Width;
            }
        }

        private IDisposable _collectionDeferRefresh;

        private void OnShowLatestVersionValueChanged(object sender, RoutedEventArgs e) {
            CollectionViewSource cvs = (CollectionViewSource)Resources["PackageCollectionSource"];
            _collectionDeferRefresh = cvs.DeferRefresh();

            cvs.GroupDescriptions.Clear();
            CheckBox box = (CheckBox)sender;
            if (box.IsChecked != true) {
                cvs.GroupDescriptions.Add(new PropertyGroupDescription("Id"));
            }

            e.Handled = true;
        }

        private void StandardDialog_Closing(object sender, CancelEventArgs e) {
            e.Cancel = true;
            CancelButton_Click(this, null);
        }

        internal void ForceClose() {
            this.Closing -= StandardDialog_Closing;
            Close();
        }
    }
}