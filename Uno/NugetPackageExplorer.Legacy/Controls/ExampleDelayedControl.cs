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
            // Property change callback executes when SetValue is called
            System.Diagnostics.Debug.WriteLine($"Title changed: {e.OldValue} -> {e.NewValue}");
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
            System.Diagnostics.Debug.WriteLine($"Subtitle changed: {e.NewValue}");
        }

        #endregion

        public ExampleDelayedControl()
        {
            // Example 1: Delayed initialization in constructor
            BeginDelayedInitialization();
            
            // Use SetValueDelayed to queue SetValue operations
            SetValueDelayed(TitleProperty, "Default Title");
            SetValueDelayed(SubtitleProperty, "Default Subtitle");
            
            // Execute all queued SetValue operations (and their callbacks) in order
            EndDelayedInitialization();
            
            // Future property changes execute immediately
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
                
                // Queue SetValue operations
                SetValueDelayed(TitleProperty, "Async Title");
                SetValueDelayed(SubtitleProperty, "Async Subtitle");
            }
            finally
            {
                // Execute all queued operations
                EndDelayedInitialization();
            }
        }
        
        /// <summary>
        /// Example showing value factory pattern for computed values
        /// </summary>
        public void InitializeWithFactory()
        {
            BeginDelayedInitialization();
            
            // Value is computed when the SetValue actually executes
            SetValueDelayed(TitleProperty, () => $"Title at {DateTime.Now:HH:mm:ss}");
            
            EndDelayedInitialization();
        }
    }
}
