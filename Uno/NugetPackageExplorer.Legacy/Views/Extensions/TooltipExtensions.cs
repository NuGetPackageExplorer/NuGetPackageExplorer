using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

using Uno.Disposables;
using Uno.Extensions;
using Uno.UI;

using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Input;

namespace NupkgExplorer.Views.Extensions
{
    public static class TooltipExtensions
    {
        /* ToolTip: ToolTipService.ToolTip with workaround for uno#6050
         * Placement: same as ToolTipService counterpart
         * PlacementTarget: same as ToolTipService counterpart
         * ToolTipReference: [private] to hold the reference of current ToolTip
         * ToolTipSubscription: [private] IDisposable to manage subscriptions */

        #region DependencyProperty: ToolTip

        public static DependencyProperty ToolTipProperty { get; } = DependencyProperty.RegisterAttached(
            "ToolTip",
            typeof(object),
            typeof(TooltipExtensions),
            new PropertyMetadata(default(object), (d, e) => d.Maybe<FrameworkElement>(control => OnToolTipChanged(control, e))));

        public static object GetToolTip(FrameworkElement obj) => (object)obj.GetValue(ToolTipProperty);
        public static void SetToolTip(FrameworkElement obj, object value) => obj.SetValue(ToolTipProperty, value);

        #endregion
        #region DependencyProperty: Placement

        public static DependencyProperty PlacementProperty { get; } = DependencyProperty.RegisterAttached(
            "Placement",
            typeof(PlacementMode),
            typeof(TooltipExtensions),
            new PropertyMetadata(PlacementMode.Top, (d, e) => d.Maybe<FrameworkElement>(control => OnPlacementChanged(control, e))));

        public static PlacementMode GetPlacement(FrameworkElement obj) => (PlacementMode)obj.GetValue(PlacementProperty);
        public static void SetPlacement(FrameworkElement obj, PlacementMode value) => obj.SetValue(PlacementProperty, value);

        #endregion
        #region DependencyProperty: PlacementTarget

        public static DependencyProperty PlacementTargetProperty { get; } = DependencyProperty.RegisterAttached(
            "PlacementTarget",
            typeof(UIElement),
            typeof(TooltipExtensions),
            new PropertyMetadata(default(UIElement), (d, e) => d.Maybe<FrameworkElement>(control => OnPlacementTargetChanged(control, e))));

        public static UIElement GetPlacementTarget(FrameworkElement obj) => (UIElement)obj.GetValue(PlacementTargetProperty);
        public static void SetPlacementTarget(FrameworkElement obj, UIElement value) => obj.SetValue(PlacementTargetProperty, value);

        #endregion
        #region DependencyProperty: ToolTipReference

        public static DependencyProperty ToolTipReferenceProperty { get; } = DependencyProperty.RegisterAttached(
            "ToolTipReference",
            typeof(ToolTip),
            typeof(TooltipExtensions),
            new PropertyMetadata(default(ToolTip)));

        public static ToolTip GetToolTipReference(FrameworkElement obj) => (ToolTip)obj.GetValue(ToolTipReferenceProperty);
        public static void SetToolTipReference(FrameworkElement obj, ToolTip? value) => obj.SetValue(ToolTipReferenceProperty, value);

        #endregion
        #region DependencyProperty: ToolTipSubscription

        private static DependencyProperty ToolTipSubscriptionProperty { get; } = DependencyProperty.RegisterAttached(
            "ToolTipSubscription",
            typeof(IDisposable),
            typeof(TooltipExtensions),
            new PropertyMetadata(default(IDisposable)));

        private static IDisposable GetToolTipSubscription(FrameworkElement obj) => (IDisposable)obj.GetValue(ToolTipSubscriptionProperty);
        private static void SetToolTipSubscription(FrameworkElement obj, IDisposable? value) => obj.SetValue(ToolTipSubscriptionProperty, value);

        #endregion

        private static void OnToolTipChanged(FrameworkElement control, DependencyPropertyChangedEventArgs e)
        {
            // empty string will treated like null here
            if (e.NewValue is null || (e.NewValue as string)?.Length == 0)
            {
                DisposePreviousToolTip();
            }
            else if (e.NewValue is ToolTip newTooltip)
            {
                var previousTooltip = GetToolTipReference(control);

                // dispose the previous tooltip
                if (previousTooltip != null && newTooltip != previousTooltip)
                {
                    DisposePreviousToolTip();
                }

                // setup new tooltip
                if (newTooltip != previousTooltip)
                {
                    SetupToolTip(newTooltip);
                }
            }
            else
            {
                var previousTooltip = GetToolTipReference(control);
                if (previousTooltip != null)
                {
                    // update the old tooltip with new content
                    previousTooltip.Content = e.NewValue;
                }
                else
                {
                    // setup a new tooltip
                    previousTooltip = new ToolTip { Content = e.NewValue };
                    SetupToolTip(previousTooltip);
                }
            }

            void SetupToolTip(ToolTip tooltip)
            {
                tooltip.Placement = GetPlacement(tooltip);
#if HAS_UNO
                tooltip.SetAnchor(GetPlacementTarget(control) ?? control);
#endif

                SetToolTipReference(control, tooltip);
                SetToolTipSubscription(control, SubscribeToEvents(control, tooltip));
            }
            void DisposePreviousToolTip()
            {
                GetToolTipSubscription(control)?.Dispose();

                SetToolTipReference(control, null);
                SetToolTipSubscription(control, null);
            }
        }

        private static void OnPlacementChanged(FrameworkElement control, DependencyPropertyChangedEventArgs e)
        {
            if (GetToolTipReference(control) is { } tooltip)
            {
                tooltip.Placement = (PlacementMode)e.NewValue;
            }
        }

        private static void OnPlacementTargetChanged(FrameworkElement control, DependencyPropertyChangedEventArgs e)
        {
            if (GetToolTipReference(control) is { } tooltip)
            {
#if HAS_UNO
                tooltip.SetAnchor(e.NewValue as UIElement ?? control);
#endif
            }
        }

        private static IDisposable SubscribeToEvents(FrameworkElement control, ToolTip tooltip)
        {
            long currentHoverId = 0;

            // event subscriptions
            if (control.IsLoaded)
            {
                SubscribeToPointerEvents(control, null);
            }
            control.Loaded += SubscribeToPointerEvents;
            control.Unloaded += UnsubscribeToPointerEvents;

            void SubscribeToPointerEvents(object sender, RoutedEventArgs? e)
            {
                control.PointerEntered += OnPointerEntered;
                control.PointerExited += OnPointerExited;
            }
            void UnsubscribeToPointerEvents(object sender, RoutedEventArgs? e)
            {
                control.PointerEntered -= OnPointerEntered;
                control.PointerExited -= OnPointerExited;
            }

            return Disposable.Create(() =>
            {
                control.Loaded -= SubscribeToPointerEvents;
                control.Unloaded -= UnsubscribeToPointerEvents;
                UnsubscribeToPointerEvents(control, null);

                tooltip.IsOpen = false;
                currentHoverId++;
            });

            // pointer event handlers
            async void OnPointerEntered(object snd, PointerRoutedEventArgs evt)
            {
                await HoverTask(++currentHoverId).ConfigureAwait(false);
            }
            void OnPointerExited(object snd, PointerRoutedEventArgs evt)
            {
                currentHoverId++;
                tooltip.IsOpen = false;
            }
            async Task HoverTask(long hoverId)
            {
#if HAS_UNO
                await Task.Delay(FeatureConfiguration.ToolTip.ShowDelay).ConfigureAwait(false);
#endif
                if (currentHoverId != hoverId)
                {
                    return;
                }

                if (control.IsLoaded)
                {
                    tooltip.IsOpen = true;

#if HAS_UNO
                    await Task.Delay(FeatureConfiguration.ToolTip.ShowDuration).ConfigureAwait(false);
#endif
                    if (currentHoverId == hoverId)
                    {
                        tooltip.IsOpen = false;
                    }
                }
            }
        }
    }
}
