using System.Collections.Concurrent;
using System.ComponentModel;
using System.ComponentModel.Composition.Hosting;
using System.Runtime.CompilerServices;

using Uno.Disposables;

using Windows.ApplicationModel.Core;
using Windows.UI.Core;

namespace NupkgExplorer.Framework.MVVM
{
    public partial class ViewModelBase : INotifyPropertyChanged
    {
        public string Title { get; protected set; } = null!;
        public string? Location { get; protected set; }

        public static CompositionContainer DefaultContainer { get; set; } = null!;

        public event PropertyChangedEventHandler? PropertyChanged;

        protected CompositionContainer Container => DefaultContainer;

        private readonly Dictionary<string, object?> _backingFields = [];
        private readonly ConcurrentQueue<PropertyChangedEventArgs> _propertyChangedQueue = new();
        private volatile bool _isInDelayedInitialization;
        private volatile bool _isReplayingEvents;
        private readonly object _replayLock = new();

        private int _propertyChangedSuppressionLevel = 0;

        protected async Task RunOnUIThread(DispatchedHandler action)
        {
            var dispatcher = CoreApplication.MainView.Dispatcher;
            if (dispatcher.HasThreadAccess)
            {
                action();
            }
            else
            {
                await dispatcher.RunAsync(CoreDispatcherPriority.Normal, action);
            }
        }

        protected T? GetProperty<T>([CallerMemberName] string? propertyName = null)
        {
            return _backingFields.TryGetValue(propertyName!, out var value) ? (T?)value : default;
        }

        protected void SetProperty<T>(T? value, [CallerMemberName] string? propertyName = null)
        {
            if (!(_backingFields.TryGetValue(propertyName!, out var oldValue) && oldValue?.Equals(value) == true))
            {
                _backingFields[propertyName!] = value;
                if (_propertyChangedSuppressionLevel == 0)
                {
                    RaisePropertyChanged(propertyName);
                }
            }
        }

        /// <summary>
        /// Raises the PropertyChanged event with proper queue handling for delayed initialization.
        /// </summary>
        private void RaisePropertyChanged(string? propertyName)
        {
            var args = new PropertyChangedEventArgs(propertyName);

            // If we're in delayed initialization or replaying events, queue the event
            if (_isInDelayedInitialization || _isReplayingEvents)
            {
                _propertyChangedQueue.Enqueue(args);
            }
            else
            {
                // Normal path: dispatch to UI thread and raise the event
                _ = RunOnUIThread(() => PropertyChanged?.Invoke(this, args));
            }
        }

        /// <summary>
        /// Prevent <see cref="PropertyChanged"/> event from being raised until the disposable is disposed.
        /// Any event that would happen during suppression of this is lost. Every disposable must be disposed before the event can be raised again.
        /// </summary>
        /// <remarks>
        /// Since <see cref="PropertyChanged"/> is dispatched to the UI-thread, its timing cannot be guaranteed.
        /// Therefore, it is recommended to set the initial values for the view-model using this suppression,
        /// in order to avoid WhenAnyValue that follows from being proc'd immediately.
        /// </remarks>
        protected IDisposable SuppressPropertyChangedNotifications()
        {
            Interlocked.Increment(ref _propertyChangedSuppressionLevel);
            return new AnonymousDisposable(
                () => Interlocked.Decrement(ref _propertyChangedSuppressionLevel)
            );
        }

        protected ICommand GetCommand(Func<Task> execute)
        {
            return new AsyncCommand(_ => execute());
        }

        protected ICommand GetCommand(Func<object?, Task> execute)
        {
            return new AsyncCommand(execute);
        }

        protected ICommand GetCommand(Action execute)
        {
            return new AsyncCommand(_ => Task.Run(execute));
        }

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
        protected async Task CompleteDelayedInitialization()
        {
            if (!_isInDelayedInitialization)
            {
                return;
            }

            // Use the lock to ensure thread-safe replay
            await Task.Run(() =>
            {
                lock (_replayLock)
                {
                    _isInDelayedInitialization = false;
                    _isReplayingEvents = true;

                    try
                    {
                        // Replay all queued events
                        while (_propertyChangedQueue.TryDequeue(out var args))
                        {
                            // Dispatch to UI thread for replay
                            _ = RunOnUIThread(() => PropertyChanged?.Invoke(this, args));
                        }
                    }
                    finally
                    {
                        _isReplayingEvents = false;
                    }
                }
            });
        }
    }
}
