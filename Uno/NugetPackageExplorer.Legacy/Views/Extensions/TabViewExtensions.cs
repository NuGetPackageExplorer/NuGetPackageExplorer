using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Text;

using Microsoft.UI.Xaml.Controls;

using Uno.Disposables;
using Uno.Extensions;

using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

namespace NupkgExplorer.Views.Extensions
{
    public static class TabViewExtensions
    {
        /* ResetSelectionWith: resets the TabView selection (to first item) or sets the TabViewItem IsSelected to true when the binding is updated to non-null value
         * ResetSelectionWithItemVisibility: resets the TabView selection (to first visible item) whenever any TabViewItem visibility changes
         * - ResetSelectionWithItemVisibilitySubscription: [private] IDisposable for managing ResetSelectionWithItemVisibility subscription
         * HideHeaderToolTip: hide mouse over tooltip on TabViewItem */

        #region DependencyProperty: ResetSelectionWith

        public static DependencyProperty ResetSelectionWithProperty { get; } = DependencyProperty.RegisterAttached(
            "ResetSelectionWith",
            typeof(object),
            typeof(TabViewExtensions),
            new PropertyMetadata(default(object), OnResetSelectionWithChanged));

        public static object GetResetSelectionWith(FrameworkElement obj) => (object)obj.GetValue(ResetSelectionWithProperty);
        public static void SetResetSelectionWith(FrameworkElement obj, object value) => obj.SetValue(ResetSelectionWithProperty, value);

        #endregion
        #region DependencyProperty: ResetSelectionWithItemVisibility

        public static DependencyProperty ResetSelectionWithItemVisibilityProperty { get; } = DependencyProperty.RegisterAttached(
            "ResetSelectionWithItemVisibility",
            typeof(bool),
            typeof(TabViewExtensions),
            new PropertyMetadata(default(bool), (d, e) => d.Maybe<TabView>(control => OnResetSelectionWithItemVisibilityChanged(control, e))));

        public static bool GetResetSelectionWithItemVisibility(TabView obj) => (bool)obj.GetValue(ResetSelectionWithItemVisibilityProperty);
        public static void SetResetSelectionWithItemVisibility(TabView obj, bool value) => obj.SetValue(ResetSelectionWithItemVisibilityProperty, value);

        #endregion
        #region DependencyProperty: ResetSelectionWithItemVisibilitySubscription

        private static DependencyProperty ResetSelectionWithItemVisibilitySubscriptionProperty { get; } = DependencyProperty.RegisterAttached(
            "ResetSelectionWithItemVisibilitySubscription",
            typeof(IDisposable),
            typeof(TabViewExtensions),
            new PropertyMetadata(default(IDisposable)));

        private static IDisposable GetResetSelectionWithItemVisibilitySubscription(TabView obj) => (IDisposable)obj.GetValue(ResetSelectionWithItemVisibilitySubscriptionProperty);
        private static void SetResetSelectionWithItemVisibilitySubscription(TabView obj, IDisposable? value) => obj.SetValue(ResetSelectionWithItemVisibilitySubscriptionProperty, value);

        #endregion
        #region DependencyProperty: HideHeaderToolTip

        public static DependencyProperty HideHeaderToolTipProperty { get; } = DependencyProperty.RegisterAttached(
            "HideHeaderToolTip",
            typeof(bool),
            typeof(TabViewExtensions),
            new PropertyMetadata(default(bool), (d, e) => d.Maybe<TabViewItem>(control => OnHideHeaderToolTipChanged(control, e))));

        public static bool GetHideHeaderToolTip(TabViewItem obj) => (bool)obj.GetValue(HideHeaderToolTipProperty);
        public static void SetHideHeaderToolTip(TabViewItem obj, bool value) => obj.SetValue(HideHeaderToolTipProperty, value);

        #endregion

        private static void OnResetSelectionWithChanged(DependencyObject sender, DependencyPropertyChangedEventArgs args)
        {
            if (sender is TabView tv) CoreImpl(tv, x => x.TabItems
                .OfType<TabViewItem>()
                .FirstOrDefault()
                ?.Apply(y => y.IsSelected = true));
            if (sender is TabViewItem tvi) CoreImpl(tvi, x => x.IsSelected = true);

            void CoreImpl<T>(T control, Action<T> action) where T : FrameworkElement
            {
                if (GetResetSelectionWith(control) is not null)
                {
                    action(control);
                }
            }
        }

        [SuppressMessage("Reliability", "CA2000:Dispose objects before losing scope", Justification = "managed via ResetSelectionWithItemVisibilitySubscription")]
        private static void OnResetSelectionWithItemVisibilityChanged(TabView control, DependencyPropertyChangedEventArgs e)
        {
            GetResetSelectionWithItemVisibilitySubscription(control)?.Dispose();
            SetResetSelectionWithItemVisibilitySubscription(control, null);

            if ((bool)e.NewValue)
            {
                // for simplicity, we assume the TabItems wont changes

                var subscriptions = new CompositeDisposable();
                foreach (var tvi in control.TabItems.OfType<TabViewItem>())
                {
                    var token = tvi.RegisterPropertyChangedCallback(UIElement.VisibilityProperty, SelectFirstVisibleTabItem);

                    subscriptions.Add(() => tvi.UnregisterPropertyChangedCallback(UIElement.VisibilityProperty, token));
                }

                SetResetSelectionWithItemVisibilitySubscription(control, subscriptions);
            }

            void SelectFirstVisibleTabItem(DependencyObject sender, DependencyProperty dp)
            {
                control.TabItems
                    .OfType<TabViewItem>()
                    .FirstOrDefault(x => x.Visibility == Visibility.Visible)
                    ?.Apply(x => x.IsSelected = true);
            }
        }

        private static void OnHideHeaderToolTipChanged(TabViewItem control, DependencyPropertyChangedEventArgs e)
        {
            if (ToolTipService.GetToolTip(control) is ToolTip tooltip)
            {
                tooltip.Opened += (s, e) => tooltip.IsOpen = false;
            }
        }
    }
}
