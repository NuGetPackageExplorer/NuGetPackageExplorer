using System;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Reactive.Linq;
using System.Windows.Input;

using Microsoft.UI.Xaml.Controls;

using Uno.Disposables;
using Uno.Extensions;
using Uno.Logging;

using Windows.UI.Xaml;
using Windows.UI.Xaml.Input;

namespace NupkgExplorer.Views.Behaviors
{
    public static class TreeViewBehavior
    {
        /* SelectedItem: exposed SelectedItem for binding
         * - EnableSelectedItemBinding: toggle
         * - IsUpdatingSelectedItem: used to prevent self-feedback loop
         * - SelectedItemBindingDisposable: Disposable for managing SelectedItem subscriptions
         * AutoToggleItemExpansion: auto expand/collapse clicked item
         * - AutoToggleItemExpansionDisposable: [private] IDisposable for managing AutoToggleItemExpansion subscription
         * DoubleClickCommand: self-explanatory;
         * - DoubleClickCommandDisposable: for managing DoubleClickCommand subscription */
        #region DependencyProperty: ItemDoubleTapCommand
        public static readonly DependencyProperty ItemDoubleTapCommandProperty = DependencyProperty.RegisterAttached(
            "ItemDoubleTapCommand",
            typeof(ICommand),
            typeof(TreeViewBehavior),
            new PropertyMetadata(default(ICommand), (d, e) => d.Maybe<TreeViewItem>(control => OnItemDoubleTapCommandChanged(control, e))));

        public static ICommand GetItemDoubleTapCommand(TreeViewItem element) => (ICommand)element.GetValue(ItemDoubleTapCommandProperty);
        public static void SetItemDoubleTapCommand(TreeViewItem element, ICommand value) => element.SetValue(ItemDoubleTapCommandProperty, value);

        [SuppressMessage("Reliability", "CA2000:Dispose objects before losing scope", Justification = "managed via DoubleTapCommandDisposable")]
        private static void OnItemDoubleTapCommandChanged(TreeViewItem control, DependencyPropertyChangedEventArgs e)
        {
            GetItemDoubleTapCommandDisposable(control)?.Dispose();
            if (e.NewValue is ICommand command)
            {
                var subscription = Observable
                    .FromEventPattern<DoubleTappedEventHandler, DoubleTappedRoutedEventArgs>(
                        h => control.DoubleTapped += h,
                        h => control.DoubleTapped -= h
                    )
                    .Select(x => x.EventArgs.OriginalSource)
                    .Subscribe(x =>
                    {
                        try
                        {
                            if (command.CanExecute(x))
                            {
                                command.Execute(x);
                            }
                        }
                        catch (Exception ex)
                        {
                            typeof(TreeViewBehavior).Log().Error("failed to execute DoubleTap command: ", ex);
                        }
                    });

                SetItemDoubleTapCommandDisposable(control, subscription);
            }
        }
        #endregion
        #region DependencyProperty: ItemDoubleTapCommandDisposable

        public static DependencyProperty ItemDoubleTapCommandDisposableProperty { get; } = DependencyProperty.RegisterAttached(
            "ItemDoubleTapCommandDisposable",
            typeof(IDisposable),
            typeof(TreeViewBehavior),
            new PropertyMetadata(default(IDisposable)));

        public static IDisposable GetItemDoubleTapCommandDisposable(TreeViewItem obj) => (IDisposable)obj.GetValue(ItemDoubleTapCommandDisposableProperty);
        public static void SetItemDoubleTapCommandDisposable(TreeViewItem obj, IDisposable value) => obj.SetValue(ItemDoubleTapCommandDisposableProperty, value);

        #endregion
        #region DependencyProperty: EnableSelectedItemBinding

        public static DependencyProperty EnableSelectedItemBindingProperty { get; } = DependencyProperty.RegisterAttached(
            "EnableSelectedItemBinding",
            typeof(bool),
            typeof(TreeViewBehavior),
            new PropertyMetadata(default, (d, e) => d.Maybe<TreeView>(control => OnEnableSelectedItemBindingChanged(control, e))));

        public static bool GetEnableSelectedItemBinding(TreeView obj) => (bool)obj.GetValue(EnableSelectedItemBindingProperty);
        public static void SetEnableSelectedItemBinding(TreeView obj, bool value) => obj.SetValue(EnableSelectedItemBindingProperty, value);

        // uwp: SelectedItem is not a DependencyProperty, therefore we cant use it for binding.
        //		normally we would use IsSelected, however this will add needless complexity to the VM
        //		so instead, we are exposing it with this behavior
        // uno: SelectedItem is implemented as a DependencyProperty, so that works.
        //		however we will be using this behavior for consistency

        private static void OnEnableSelectedItemBindingChanged(TreeView sender, DependencyPropertyChangedEventArgs e)
        {
            GetSelectedItemBindingDisposable(sender)?.Dispose();

            if (GetEnableSelectedItemBinding(sender))
            {
                if (sender.SelectionMode != TreeViewSelectionMode.Single)
                {
                    typeof(TreeViewBehavior).Log().Warn($"{nameof(SelectedItemProperty)} should be used with single selection mode (current mode: {sender.SelectionMode}).");
                }

                sender.ItemInvoked += UpdateSelectedItemBinding;
                SetSelectedItemBindingDisposable(sender, Disposable.Create(() =>
                    sender.ItemInvoked -= UpdateSelectedItemBinding
                ));
            }
        }

        private static void UpdateSelectedItemBinding(TreeView sender, TreeViewItemInvokedEventArgs args)
        {
            // note: this event fired before SelectedItem is updated
            SetIsUpdatingSelectedItem(sender, true);
            SetSelectedItem(sender, args.InvokedItem);
            SetIsUpdatingSelectedItem(sender, false);
        }
        #endregion
        #region DependencyProperty: IsUpdatingSelectedItem

        public static DependencyProperty IsUpdatingSelectedItemProperty { get; } = DependencyProperty.RegisterAttached(
            "IsUpdatingSelectedItem",
            typeof(bool),
            typeof(TreeViewBehavior),
            new PropertyMetadata(default));

        public static bool GetIsUpdatingSelectedItem(TreeView obj) => (bool)obj.GetValue(IsUpdatingSelectedItemProperty);
        public static void SetIsUpdatingSelectedItem(TreeView obj, bool value) => obj.SetValue(IsUpdatingSelectedItemProperty, value);

        #endregion
        #region DependencyProperty: SelectedItem

        public static DependencyProperty SelectedItemProperty { get; } = DependencyProperty.RegisterAttached(
            "SelectedItem",
            typeof(object),
            typeof(TreeViewBehavior),
            new PropertyMetadata(default, (d, e) => d.Maybe<TreeView>(control => OnSelectedItemChanged(control, e))));

        public static object GetSelectedItem(TreeView obj) => (object)obj.GetValue(SelectedItemProperty);
        public static void SetSelectedItem(TreeView obj, object value) => obj.SetValue(SelectedItemProperty, value);
        private static void OnSelectedItemChanged(TreeView sender, DependencyPropertyChangedEventArgs e)
        {
            // sync value if not coming from UpdateSelectedItemBinding
            if (!GetIsUpdatingSelectedItem(sender))
            {
                sender.SelectedItem = e.NewValue;
            }
        }
        #endregion
        #region DependencyProperty: SelectedItemBindingDisposable

        public static DependencyProperty SelectedItemBindingDisposableProperty { get; } = DependencyProperty.RegisterAttached(
            "SelectedItemBindingDisposable",
            typeof(IDisposable),
            typeof(TreeViewBehavior),
            new PropertyMetadata(default));

        public static IDisposable GetSelectedItemBindingDisposable(TreeView obj) => (IDisposable)obj.GetValue(SelectedItemBindingDisposableProperty);
        public static void SetSelectedItemBindingDisposable(TreeView obj, IDisposable value) => obj.SetValue(SelectedItemBindingDisposableProperty, value);

        #endregion
        #region DependencyProperty: AutoToggleItemExpansion

        public static DependencyProperty AutoToggleItemExpansionProperty { get; } = DependencyProperty.RegisterAttached(
        "AutoToggleItemExpansion",
        typeof(bool),
        typeof(TreeViewBehavior),
        new PropertyMetadata(default(bool), (d, e) => d.Maybe<TreeView>(control => OnAutoToggleItemExpansionChanged(control, e))));

        public static bool GetAutoToggleItemExpansion(TreeView obj) => (bool)obj.GetValue(AutoToggleItemExpansionProperty);
        public static void SetAutoToggleItemExpansion(TreeView obj, bool value) => obj.SetValue(AutoToggleItemExpansionProperty, value);

        [SuppressMessage("Reliability", "CA2000:Dispose objects before losing scope", Justification = "managed through ")]
        private static void OnAutoToggleItemExpansionChanged(TreeView control, DependencyPropertyChangedEventArgs e)
        {
            GetAutoToggleItemExpansionDisposable(control)?.Dispose();
            if ((bool)e.NewValue)
            {
                control.ItemInvoked += ToggleExpansion;
                SetAutoToggleItemExpansionDisposable(control, Disposable.Create(() =>
                    control.ItemInvoked -= ToggleExpansion
                ));
            }

            void ToggleExpansion(TreeView sender, TreeViewItemInvokedEventArgs args)
            {
                try
                {
                    if (args.InvokedItem != null)
                    {
                        var container = args.InvokedItem as TreeViewItem ?? sender.ContainerFromItem(args.InvokedItem) as TreeViewItem;
                        if (container != null)
                        {
                            container.IsExpanded = !container.IsExpanded;
                        }
                    }
                }
                catch (Exception e)
                {
                    typeof(TreeViewBehavior).Log().ErrorIfEnabled(() => "failed to auto expand selected item: ", e);
                }
            }
        }

        #endregion
        #region DependencyProperty: AutoToggleItemExpansionDisposable

        private static DependencyProperty AutoToggleItemExpansionDisposableProperty { get; } = DependencyProperty.RegisterAttached(
            "AutoToggleItemExpansionDisposable",
            typeof(IDisposable),
            typeof(TreeViewBehavior),
            new PropertyMetadata(default(IDisposable)));

        private static IDisposable GetAutoToggleItemExpansionDisposable(TreeView obj) => (IDisposable)obj.GetValue(AutoToggleItemExpansionDisposableProperty);
        private static void SetAutoToggleItemExpansionDisposable(TreeView obj, IDisposable value) => obj.SetValue(AutoToggleItemExpansionDisposableProperty, value);

        #endregion
    }
}