using Microsoft.UI.Xaml;
using NupkgExplorer.Framework.MVVM;

namespace NupkgExplorer.Controls
{
    /// <summary>
    /// Example control demonstrating delayed initialization with dependency properties.
    /// This file serves as a reference implementation and can be removed or used as a template.
    /// </summary>
    public class ExampleDelayedControl : ControlBase
    {
        #region Title DependencyProperty

        public static readonly DependencyProperty TitleProperty =
            DependencyProperty.Register(
                nameof(Title),
                typeof(string),
                typeof(ExampleDelayedControl),
                new PropertyMetadata(null, OnTitleChanged));

        public string? Title
        {
            get => (string?)GetValue(TitleProperty);
            set => SetValue(TitleProperty, value);
        }

        private static void OnTitleChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var control = (ExampleDelayedControl)d;
            // Use HandlePropertyChanged to enable queueing during delayed initialization
            control.HandlePropertyChanged(() => control.OnTitleChangedImpl(e.OldValue as string, e.NewValue as string));
        }

        private void OnTitleChangedImpl(string? oldValue, string? newValue)
        {
            // Property change logic here
            // During delayed initialization, this will be queued
            // After initialization, this executes immediately
            System.Diagnostics.Debug.WriteLine($"Title changed: {oldValue} -> {newValue}");
        }

        #endregion

        #region Subtitle DependencyProperty

        public static readonly DependencyProperty SubtitleProperty =
            DependencyProperty.Register(
                nameof(Subtitle),
                typeof(string),
                typeof(ExampleDelayedControl),
                new PropertyMetadata(null, OnSubtitleChanged));

        public string? Subtitle
        {
            get => (string?)GetValue(SubtitleProperty);
            set => SetValue(SubtitleProperty, value);
        }

        private static void OnSubtitleChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var control = (ExampleDelayedControl)d;
            control.HandlePropertyChanged(() => control.OnSubtitleChangedImpl());
        }

        private void OnSubtitleChangedImpl()
        {
            System.Diagnostics.Debug.WriteLine($"Subtitle changed: {Subtitle}");
        }

        #endregion

        public ExampleDelayedControl()
        {
            // Example 1: Delayed initialization in constructor
            BeginDelayedInitialization();
            
            // Set multiple properties - callbacks will be queued
            Title = "Default Title";
            Subtitle = "Default Subtitle";
            
            // Replay all queued callbacks in order
            EndDelayedInitialization();
            
            // Future property changes will execute immediately
        }

        /// <summary>
        /// Example method showing async initialization pattern
        /// </summary>
        public async Task InitializeAsync()
        {
            BeginDelayedInitialization();
            
            try
            {
                // Simulate async data loading
                await Task.Delay(100);
                
                Title = "Async Title";
                Subtitle = "Async Subtitle";
            }
            finally
            {
                EndDelayedInitialization();
            }
        }
    }
}
