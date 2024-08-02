using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Reactive.Linq;
using System.Text;
using System.Windows.Input;

using NupkgExplorer.Views.Helpers;

using Uno.Disposables;
using Uno.Extensions;
using Uno.Logging;

using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Data;

namespace NupkgExplorer.Views.Extensions
{
    public static class ListViewExtensions
    {
        /* AddIncrementallyLoadingSupport: add support for ISupportIncrementalLoading on wasm & skia
         * - IsIncrementallyLoading: [private] flag to prevent re-entrancy
         * DoubleClickCommand: self-explanatory; note: must also set IsItemClickEnabled=True
         * - DoubleClickCommandDisposable: for managing DoubleClickCommand subscription */

        #region DependencyProperty: AddIncrementallyLoadingSupport

        public static DependencyProperty AddIncrementallyLoadingSupportProperty { get; } = DependencyProperty.RegisterAttached(
            "AddIncrementallyLoadingSupport",
            typeof(bool),
            typeof(ListViewExtensions),
            new PropertyMetadata(default(bool), (d, e) => d.Maybe<ListView>(control => OnAddIncrementallyLoadingSupportChanged(control, e))));

        public static bool GetAddIncrementallyLoadingSupport(ListView obj) => (bool)obj.GetValue(AddIncrementallyLoadingSupportProperty);
        public static void SetAddIncrementallyLoadingSupport(ListView obj, bool value) => obj.SetValue(AddIncrementallyLoadingSupportProperty, value);

        #endregion
        #region DependencyProperty: IsIncrementallyLoading

        private static DependencyProperty IsIncrementallyLoadingProperty { get; } = DependencyProperty.RegisterAttached(
            "IsIncrementallyLoading",
            typeof(bool),
            typeof(ListViewExtensions),
            new PropertyMetadata(default(bool)));

        private static bool GetIsIncrementallyLoading(ListView obj) => (bool)obj.GetValue(IsIncrementallyLoadingProperty);
        private static void SetIsIncrementallyLoading(ListView obj, bool value) => obj.SetValue(IsIncrementallyLoadingProperty, value);

        #endregion
        #region DependencyProperty: DoubleClickCommand

        public static DependencyProperty DoubleClickCommandProperty { get; } = DependencyProperty.RegisterAttached(
            "DoubleClickCommand",
            typeof(ICommand),
            typeof(ListViewExtensions),
            new PropertyMetadata(default(ICommand), (d, e) => d.Maybe<ListView>(control => OnDoubleClickCommandChanged(control, e))));

        public static ICommand GetDoubleClickCommand(ListView obj) => (ICommand)obj.GetValue(DoubleClickCommandProperty);
        public static void SetDoubleClickCommand(ListView obj, ICommand value) => obj.SetValue(DoubleClickCommandProperty, value);

        #endregion
        #region DependencyProperty: DoubleClickCommandDisposable

        public static DependencyProperty DoubleClickCommandDisposableProperty { get; } = DependencyProperty.RegisterAttached(
            "DoubleClickCommandDisposable",
            typeof(IDisposable),
            typeof(ListViewExtensions),
            new PropertyMetadata(default(IDisposable)));

        public static IDisposable GetDoubleClickCommandDisposable(ListView obj) => (IDisposable)obj.GetValue(DoubleClickCommandDisposableProperty);
        public static void SetDoubleClickCommandDisposable(ListView obj, IDisposable value) => obj.SetValue(DoubleClickCommandDisposableProperty, value);

        #endregion

        private static void OnAddIncrementallyLoadingSupportChanged(ListView control, DependencyPropertyChangedEventArgs e)
        {
#if __WASM__ || __SKIA__
            if ((bool)e.NewValue)
            {
                if (control.IsLoaded)
                {
                    InstallIncrementalLoadingWorkaround(control, null!);
                }
                else
                {
                    control.Loaded += InstallIncrementalLoadingWorkaround;
                }
            }
#endif
        }

        [SuppressMessage("Reliability", "CA2000:Dispose objects before losing scope", Justification = "managed via DoubleClickCommandDisposable")]
        private static void OnDoubleClickCommandChanged(ListView control, DependencyPropertyChangedEventArgs e)
        {
            const int MaxDoubleClickDelay = 500; // in ms
            const int DoubleClickCutOffThreshold = 150; // ms

            GetDoubleClickCommandDisposable(control)?.Dispose();
            if (e.NewValue is ICommand command)
            {
                var subscriptions = new CompositeDisposable();

                var clicks = Observable
                    .FromEventPattern<ItemClickEventHandler, ItemClickEventArgs>(
                        h => control.ItemClick += h,
                        h => control.ItemClick -= h
                    )
                    .Select(x => x.EventArgs.ClickedItem)
                    .Publish();
                clicks.Connect().DisposeWith(subscriptions);

                Observable
                    // capture double-click on same item within MaxDoubleClickDelay
                    .Zip(clicks.TimeInterval().Skip(1), clicks, (tsx, prev) => new { tsx.Interval, tsx.Value, Previous = prev })
                    .Where(x => x.Interval < TimeSpan.FromMilliseconds(MaxDoubleClickDelay) && x.Value == x.Previous)
                    // reduce 3...n-consecutives clicks into a single double-click, instead of (n-2 ones)
                    .Throttle(TimeSpan.FromMilliseconds(DoubleClickCutOffThreshold))
                    .Subscribe(x =>
                    {
                        try
                        {
                            control.DispatcherQueue.TryEnqueue(() =>
                            {
                                if (command.CanExecute(x.Value))
                                {
                                    command.Execute(x.Value);
                                }
                            });
                        }
                        catch (Exception ex)
                        {
                            typeof(ListViewExtensions).Log().Error("failed to execute DoubleClick command: ", ex);
                        }
                    })
                    .DisposeWith(subscriptions);

                SetDoubleClickCommandDisposable(control, subscriptions);
            }
        }

#if __WASM__ || __SKIA__
        private static void InstallIncrementalLoadingWorkaround(object sender, RoutedEventArgs _)
        {
            var lv = (ListView)sender;
            var sv = VisualTreeHelperEx.GetFirstDescendant<ScrollViewer>(lv);

            sv.ViewChanged += async (s, e) =>
            {
                if (lv.ItemsSource is not ISupportIncrementalLoading source) return;
                if (lv.Items.Count > 0 && !source.HasMoreItems) return;
                if (GetIsIncrementallyLoading(lv)) return;

                // note: for simplicity, we assume the ItemsPanel is stacked vertically

                // try to load more when there is less than half a page;
                // sv.VerticalOffset only represents the top of rendered area,
                // we need another sv.ViewportHeight (or 1.0 after division) to get to the bottom
                if (((sv.ExtentHeight - sv.VerticalOffset) / sv.ViewportHeight) - 1.0 <= 0.5)
                {
                    try
                    {
                        SetIsIncrementallyLoading(lv, true);
                        await source.LoadMoreItemsAsync(1);
                    }
                    catch (Exception ex)
                    {
                        typeof(ListViewExtensions).Log().Error("failed to load more items: ", ex);
                    }
                    finally
                    {
                        SetIsIncrementallyLoading(lv, false);
                    }
                }
            };
        }
#endif
    }
}
