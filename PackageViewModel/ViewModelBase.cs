using System.Collections.Concurrent;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace PackageExplorerViewModel
{
    public abstract class ViewModelBase : INotifyPropertyChanged
    {
        private readonly ConcurrentQueue<PropertyChangedEventArgs> _propertyChangedQueue = new();
        private volatile bool _isInDelayedInitialization;
        private volatile bool _isReplayingEvents;
        private readonly object _replayLock = new();

        public event PropertyChangedEventHandler? PropertyChanged = static delegate { };

        /// <summary>
        /// Starts delayed initialization mode. During this phase, property change events are queued
        /// instead of being raised immediately. Call <see cref="CompleteDelayedInitialization"/> to 
        /// replay all queued events in order.
        /// </summary>
        protected void BeginDelayedInitialization()
        {
            _isInDelayedInitialization = true;
        }

        /// <summary>
        /// Completes delayed initialization and replays all queued property change events in the order
        /// they were captured. Any events raised during replay are added to the queue to maintain proper ordering.
        /// Once the queue is drained, events are processed directly going forward.
        /// </summary>
        protected void CompleteDelayedInitialization()
        {
            if (!_isInDelayedInitialization)
            {
                return;
            }

            lock (_replayLock)
            {
                _isInDelayedInitialization = false;
                _isReplayingEvents = true;

                try
                {
                    // Replay all queued events
                    // Process events in batches to prevent infinite loops from event handlers
                    // that trigger new property changes during replay
                    var processedCount = 0;
                    var maxEvents = 10000; // Safety limit to prevent infinite loops
                    
                    while (_propertyChangedQueue.TryDequeue(out var args) && processedCount < maxEvents)
                    {
                        processedCount++;
                        // Directly invoke the event to replay it
                        PropertyChanged!(this, args);
                    }
                }
                finally
                {
                    _isReplayingEvents = false;
                }
            }
        }

        protected virtual void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        {
            var args = new PropertyChangedEventArgs(propertyName);

            // If we're in delayed initialization or replaying events, queue the event
            if (_isInDelayedInitialization || _isReplayingEvents)
            {
                _propertyChangedQueue.Enqueue(args);
            }
            else
            {
                // Normal path: raise the event immediately
                PropertyChanged!(this, args);
            }
        }
    }
}
