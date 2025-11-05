using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

namespace NupkgExplorer.Framework.MVVM
{
    /// <summary>
    /// Base class for custom Uno controls with support for delayed dependency property initialization.
    /// Provides queue-based mechanism to capture and replay dependency property changes during initialization.
    /// </summary>
    public abstract class ControlBase : Control
    {
        private Queue<Action>? _propertyChangeQueue;
        private bool _isInitialized = true; // Default to initialized (existing behavior)
        private bool _isReplaying;

        /// <summary>
        /// Begins delayed initialization mode. Dependency property change callbacks will be queued instead of being invoked immediately.
        /// </summary>
        protected void BeginDelayedInitialization()
        {
            if (!_isInitialized)
            {
                throw new InvalidOperationException("Delayed initialization has already been started.");
            }

            _isInitialized = false;
            _propertyChangeQueue = new Queue<Action>();
        }

        /// <summary>
        /// Ends delayed initialization mode and replays all queued dependency property change callbacks in order.
        /// Callbacks that trigger during replay are added to the queue to maintain proper ordering.
        /// Once the queue is drained, callbacks are invoked directly.
        /// </summary>
        protected void EndDelayedInitialization()
        {
            if (_isInitialized)
            {
                throw new InvalidOperationException("Delayed initialization has not been started or has already been completed.");
            }

            if (_propertyChangeQueue is null)
            {
                throw new InvalidOperationException("Property change queue is not initialized.");
            }

            _isInitialized = true;
            _isReplaying = true;

            try
            {
                // Process property changes until the queue is empty
                while (_propertyChangeQueue.Count > 0)
                {
                    var callback = _propertyChangeQueue.Dequeue();
                    callback();
                }
            }
            finally
            {
                _isReplaying = false;
                _propertyChangeQueue = null; // Queue is no longer needed
            }
        }

        /// <summary>
        /// Gets a value indicating whether the control is currently in delayed initialization mode.
        /// </summary>
        protected bool IsDelayedInitializationActive => !_isInitialized;

        /// <summary>
        /// Handles dependency property changes with support for delayed initialization.
        /// Call this method from your property changed callbacks to enable queueing during initialization.
        /// </summary>
        /// <param name="callback">The callback to invoke for the property change.</param>
        protected void HandlePropertyChanged(Action callback)
        {
            if (callback == null)
            {
                throw new ArgumentNullException(nameof(callback));
            }

            // If we're not initialized (delayed initialization mode) or currently replaying changes,
            // queue the callback instead of invoking it immediately
            if (!_isInitialized || _isReplaying)
            {
                _propertyChangeQueue?.Enqueue(callback);
            }
            else
            {
                // Normal processing: invoke the callback immediately
                callback();
            }
        }
    }
}
