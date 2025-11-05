using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace PackageExplorerViewModel
{
    public abstract class ViewModelBase : INotifyPropertyChanged
    {
        private Queue<PropertyChangedEventArgs>? _eventQueue;
        private bool _isInitialized = true; // Default to initialized (existing behavior)
        private bool _isReplaying;

        public event PropertyChangedEventHandler? PropertyChanged = static delegate { };

        /// <summary>
        /// Begins delayed initialization mode. Property change events will be queued instead of being raised immediately.
        /// </summary>
        protected void BeginDelayedInitialization()
        {
            if (!_isInitialized)
            {
                throw new InvalidOperationException("Delayed initialization has already been started.");
            }

            _isInitialized = false;
            _eventQueue = new Queue<PropertyChangedEventArgs>();
        }

        /// <summary>
        /// Ends delayed initialization mode and replays all queued property change events in order.
        /// Events that occur during replay are added to the queue to maintain proper ordering.
        /// Once the queue is drained, events are processed directly.
        /// </summary>
        protected void EndDelayedInitialization()
        {
            if (_isInitialized)
            {
                throw new InvalidOperationException("Delayed initialization has not been started or has already been completed.");
            }

            if (_eventQueue is null)
            {
                throw new InvalidOperationException("Event queue is not initialized.");
            }

            _isInitialized = true;
            _isReplaying = true;

            try
            {
                // Process events until the queue is empty
                while (_eventQueue.Count > 0)
                {
                    var args = _eventQueue.Dequeue();
                    RaisePropertyChanged(args);
                }
            }
            finally
            {
                _isReplaying = false;
                _eventQueue = null; // Queue is no longer needed
            }
        }

        /// <summary>
        /// Gets a value indicating whether the view model is currently in delayed initialization mode.
        /// </summary>
        protected bool IsDelayedInitializationActive => !_isInitialized;

        protected virtual void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        {
            var args = new PropertyChangedEventArgs(propertyName);

            // If we're not initialized (delayed initialization mode) or currently replaying events,
            // queue the event instead of raising it immediately
            if (!_isInitialized || _isReplaying)
            {
                _eventQueue?.Enqueue(args);
            }
            else
            {
                // Normal processing: raise the event immediately
                RaisePropertyChanged(args);
            }
        }

        private void RaisePropertyChanged(PropertyChangedEventArgs args)
        {
            PropertyChanged!(this, args);
        }
    }
}
