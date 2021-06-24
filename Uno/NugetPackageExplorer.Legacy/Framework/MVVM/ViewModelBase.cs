using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.Composition.Hosting;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Input;

using Uno.Disposables;

using Windows.ApplicationModel.Core;
using Windows.UI.Core;
using Windows.UI.ViewManagement;

namespace NupkgExplorer.Framework.MVVM
{
	public class ViewModelBase : INotifyPropertyChanged
	{
        public string Title { get; protected set; }
        public string? Location { get; protected set; }

		public static CompositionContainer DefaultContainer { get; set; }

		public event PropertyChangedEventHandler PropertyChanged;

		protected CompositionContainer Container => DefaultContainer;

		private readonly IDictionary<string, object> _backingFields = new Dictionary<string, object>();

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

		protected T GetProperty<T>([CallerMemberName] string propertyName = null)
		{
			return _backingFields.TryGetValue(propertyName, out var value) ? (T)value : default;
		}

		protected void SetProperty<T>(T value, [CallerMemberName] string propertyName = null)
		{
			if (!(_backingFields.TryGetValue(propertyName, out var oldValue) && oldValue?.Equals(value) == true))
			{
				_backingFields[propertyName] = value;
                if (_propertyChangedSuppressionLevel == 0)
                {
                    _ = RunOnUIThread(() => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName)));
                }
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

		protected ICommand GetCommand(Func<object, Task> execute)
		{
			return new AsyncCommand(execute);
		}

		protected ICommand GetCommand(Action execute)
		{
			return new AsyncCommand(_ => Task.Run(execute));
		}
	}
}
