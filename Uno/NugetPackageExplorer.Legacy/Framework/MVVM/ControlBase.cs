using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

namespace NupkgExplorer.Framework.MVVM
{
    /// <summary>
    /// Base class for custom Uno controls with support for delayed dependency property initialization.
    /// Provides queue-based mechanism to defer dependency property SetValue operations during initialization.
    /// </summary>
    public abstract class ControlBase : Control
    {
        private Queue<Action>? _setValueQueue;
        private bool _isInitialized = true; // Default to initialized (existing behavior)
        private bool _isReplaying;

        /// <summary>
        /// Begins delayed initialization mode. Dependency property SetValue operations will be queued instead of being executed immediately.
        /// </summary>
        protected void BeginDelayedInitialization()
        {
            if (!_isInitialized)
            {
                throw new InvalidOperationException("Delayed initialization has already been started.");
            }

            _isInitialized = false;
            _setValueQueue = new Queue<Action>();
        }

        /// <summary>
        /// Ends delayed initialization mode and executes all queued SetValue operations in order.
        /// SetValue operations that trigger during replay are added to the queue to maintain proper ordering.
        /// Once the queue is drained, SetValue operations execute directly.
        /// </summary>
        protected void EndDelayedInitialization()
        {
            if (_isInitialized)
            {
                throw new InvalidOperationException("Delayed initialization has not been started or has already been completed.");
            }

            if (_setValueQueue is null)
            {
                throw new InvalidOperationException("SetValue queue is not initialized.");
            }

            _isInitialized = true;
            _isReplaying = true;

            try
            {
                // Process SetValue operations until the queue is empty
                while (_setValueQueue.Count > 0)
                {
                    var setValueAction = _setValueQueue.Dequeue();
                    setValueAction();
                }
            }
            finally
            {
                _isReplaying = false;
                _setValueQueue = null; // Queue is no longer needed
            }
        }

        /// <summary>
        /// Gets a value indicating whether the control is currently in delayed initialization mode.
        /// </summary>
        protected bool IsDelayedInitializationActive => !_isInitialized;

        /// <summary>
        /// Sets the value of a dependency property with support for delayed initialization.
        /// Use this instead of SetValue() directly to enable queueing during initialization.
        /// </summary>
        /// <param name="dp">The dependency property to set.</param>
        /// <param name="value">The new value.</param>
        protected void SetValueDelayed(DependencyProperty dp, object? value)
        {
            if (dp == null)
            {
                throw new ArgumentNullException(nameof(dp));
            }

            // If we're not initialized (delayed initialization mode) or currently replaying,
            // queue the SetValue operation instead of executing it immediately
            if (!_isInitialized || _isReplaying)
            {
                _setValueQueue?.Enqueue(() => SetValue(dp, value));
            }
            else
            {
                // Normal processing: execute SetValue immediately
                SetValue(dp, value);
            }
        }

        /// <summary>
        /// Sets the value of a dependency property with support for delayed initialization.
        /// This overload accepts a value factory that is invoked when the SetValue operation is executed.
        /// </summary>
        /// <param name="dp">The dependency property to set.</param>
        /// <param name="valueFactory">A function that returns the value to set.</param>
        protected void SetValueDelayed(DependencyProperty dp, Func<object?> valueFactory)
        {
            if (dp == null)
            {
                throw new ArgumentNullException(nameof(dp));
            }

            if (valueFactory == null)
            {
                throw new ArgumentNullException(nameof(valueFactory));
            }

            // If we're not initialized (delayed initialization mode) or currently replaying,
            // queue the SetValue operation instead of executing it immediately
            if (!_isInitialized || _isReplaying)
            {
                _setValueQueue?.Enqueue(() => SetValue(dp, valueFactory()));
            }
            else
            {
                // Normal processing: execute SetValue immediately
                SetValue(dp, valueFactory());
            }
        }
    }
}
