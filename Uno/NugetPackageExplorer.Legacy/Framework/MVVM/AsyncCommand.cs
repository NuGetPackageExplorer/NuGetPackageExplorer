using System;
using System.Collections.Generic;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;

namespace NupkgExplorer.Framework.MVVM
{
	public class AsyncCommand : ICommand
	{
		public event EventHandler CanExecuteChanged;

		public Func<object, Task> _execute;
		public bool _canExecute;
		public Subject<bool> _isExecuting;

		public AsyncCommand(Func<object, Task> execute, IObservable<bool> observeCanExecute = null)
		{
			_execute = execute;
			_isExecuting = new Subject<bool>();

			Observable
				.CombineLatest(_isExecuting.StartWith(false), observeCanExecute ?? Observable.Return(true), (executing, canExecute) => canExecute && !executing)
				.StartWith(false)
				.Subscribe(x =>
				{
					_canExecute = x;
					CanExecuteChanged?.Invoke(this, default);
				});
		}

		public bool CanExecute(object parameter) => _canExecute;

		void ICommand.Execute(object parameter) => _ = Execute(parameter);

		public async Task Execute(object parameter)
		{
			if (!_canExecute) return;

			try
			{
				_isExecuting.OnNext(true);
				await _execute(parameter);
			}
			finally
			{
				_isExecuting.OnNext(false);
			}
		}
	}
}
