using System;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Reactive.Linq;
using System.Windows.Input;

using Microsoft.UI.Xaml.Controls;

using Uno.Disposables;
using Uno.Extensions;
using Uno.Logging;

using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Input;

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
		#region DependencyProperty: EnableSelectedItemBinding

		public static DependencyProperty EnableSelectedItemBindingProperty { get; } = DependencyProperty.RegisterAttached(
			"EnableSelectedItemBinding",
			typeof(bool),
			typeof(TreeViewBehavior),
			new PropertyMetadata(default, (d, e) => d.Maybe<TreeView>(control => OnEnableSelectedItemBindingChanged(control, e))));

		public static bool GetEnableSelectedItemBinding(TreeView obj) => (bool)obj.GetValue(EnableSelectedItemBindingProperty);
		public static void SetEnableSelectedItemBinding(TreeView obj, bool value) => obj.SetValue(EnableSelectedItemBindingProperty, value);

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
        #region DependencyProperty: DoubleClickCommand

        public static DependencyProperty DoubleClickCommandProperty { get; } = DependencyProperty.RegisterAttached(
            "DoubleClickCommand",
            typeof(ICommand),
            typeof(TreeViewBehavior),
            new PropertyMetadata(default(ICommand), (d, e) => d.Maybe<TreeView>(control => OnDoubleClickCommandChanged(control, e))));

        public static ICommand GetDoubleClickCommand(TreeView obj) => (ICommand)obj.GetValue(DoubleClickCommandProperty);
        public static void SetDoubleClickCommand(TreeView obj, ICommand value) => obj.SetValue(DoubleClickCommandProperty, value);

        #endregion
        #region DependencyProperty: DoubleClickCommandDisposable

        public static DependencyProperty DoubleClickCommandDisposableProperty { get; } = DependencyProperty.RegisterAttached(
            "DoubleClickCommandDisposable",
            typeof(IDisposable),
            typeof(TreeViewBehavior),
            new PropertyMetadata(default(IDisposable)));

        public static IDisposable GetDoubleClickCommandDisposable(TreeView obj) => (IDisposable)obj.GetValue(DoubleClickCommandDisposableProperty);
        public static void SetDoubleClickCommandDisposable(TreeView obj, IDisposable value) => obj.SetValue(DoubleClickCommandDisposableProperty, value);

        #endregion

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

		private static void OnSelectedItemChanged(TreeView sender, DependencyPropertyChangedEventArgs e)
		{
			// sync value if not coming from UpdateSelectedItemBinding
			if (!GetIsUpdatingSelectedItem(sender))
			{
				sender.SelectedItem = e.NewValue;
			}
		}

		private static void UpdateSelectedItemBinding(TreeView sender, TreeViewItemInvokedEventArgs args)
		{
			// note: this event fired before SelectedItem is updated
			SetIsUpdatingSelectedItem(sender, true);
			SetSelectedItem(sender, args.InvokedItem);
			SetIsUpdatingSelectedItem(sender, false);
		}

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

        [SuppressMessage("Reliability", "CA2000:Dispose objects before losing scope", Justification = "managed via DoubleClickCommandDisposable")]
        private static void OnDoubleClickCommandChanged(TreeView control, DependencyPropertyChangedEventArgs e)
        {
            GetDoubleClickCommandDisposable(control)?.Dispose();
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
                            typeof(TreeViewBehavior).Log().Error("failed to execute DoubleClick command: ", ex);
                        }
                    });

                SetDoubleClickCommandDisposable(control, subscription);
            }
        }
	}
}
