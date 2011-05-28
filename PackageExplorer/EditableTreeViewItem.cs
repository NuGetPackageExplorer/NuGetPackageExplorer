using System.Windows;
using System.Windows.Controls;

namespace PackageExplorer {
    public class EditableTreeViewItem : TreeViewItem {

        private TextBox _editTextBox;

        public EditableTreeViewItem() {
            DefaultStyleKey = typeof(EditableTreeViewItem);
        }

        public override void OnApplyTemplate() {
            base.OnApplyTemplate();

            _editTextBox = GetTemplateChild("PART_EditHeader") as TextBox;
        }

        public bool IsEditMode {
            get { return (bool)GetValue(IsEditModeProperty); }
            set { SetValue(IsEditModeProperty, value); }
        }

        // Using a DependencyProperty as the backing store for IsEditMode.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty IsEditModeProperty =
            DependencyProperty.Register("IsEditMode", typeof(bool), typeof(EditableTreeViewItem), new UIPropertyMetadata(false, new PropertyChangedCallback(OnIsEditModePropertyChanged)));

        private static void OnIsEditModePropertyChanged(DependencyObject sender, DependencyPropertyChangedEventArgs args) {
            ((EditableTreeViewItem)sender).OnIsEditModeChanged((bool)args.NewValue);
        }

        private void OnIsEditModeChanged(bool newValue) {
            if (_editTextBox != null && newValue) {
                _editTextBox.Focus();
            }
        }

        protected override DependencyObject GetContainerForItemOverride() {
            return new EditableTreeViewItem();
        }
    }
}
