using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive.Concurrency;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using System.Text;
using System.Threading;
using System.Windows.Input;
using NupkgExplorer.Framework.MVVM;

using Uno.Disposables;
using Uno.Extensions;

namespace NupkgExplorer.Presentation.Dialogs
{
	public class DownloadProgressDialogViewModel : ViewModelBase, IProgress<(long ReceivedBytes, long? TotalBytes)>
	{
		private readonly ISubject<(long ReceivedBytes, long? TotalBytes)> _progressSubject;
		private readonly CancellationDisposable _downloadCts;

		public string PackageName
		{
			get => GetProperty<string>();
			set => SetProperty(value);
		}

		public string PackageVersion
		{
			get => GetProperty<string>();
			set => SetProperty(value);
		}

		public string Ellipsis
		{
			get => GetProperty<string>();
			set => SetProperty(value);
		}

		public double? Progress
		{
			get => GetProperty<double?>();
			set => SetProperty(value);
		}

		public string ReceivedTest
		{
			get => GetProperty<string>();
			set => SetProperty(value);
		}

		public long? Received
		{
			get => GetProperty<long>();
			set => SetProperty(value);
		}

		public long? Total
		{
			get => GetProperty<long?>();
			set => SetProperty(value);
		}

		public ICommand CancelDownloadCommand => this.GetCommand(CancelDownload);

		public DownloadProgressDialogViewModel(string packageName, string packageVersion, CancellationDisposable downloadCts)
		{
			this._progressSubject = new ReplaySubject<(long ReceivedBytes, long? TotalBytes)>(1);
			this._downloadCts = downloadCts;
			this.PackageName = packageName;
			this.PackageVersion = packageVersion;

            var disposable = new CompositeDisposable();

#if WINDOWS_UWP || HAS_UNO_SKIA // disabled for other platforms due to #70
			var progressDisposable = _progressSubject
				// limit update frequency
				.Buffer(TimeSpan.FromMilliseconds(150))
				.Where(g => g.Any())
				.Select(g => g.Last())
				.Subscribe(x =>
				{
					Received = x.ReceivedBytes;
					Total = x.TotalBytes;
					Progress = 100.0 * x.ReceivedBytes / x.TotalBytes;
				});

            disposable.Add(progressDisposable);
#else
            // Required to ensure that the ProgressBar will not animate needlessly.
            var ellipsisDisposable = Observable.Interval(TimeSpan.FromMilliseconds(150))
				.Select(x => x % 5)
				.Where(x => x <= 3) // empty,1,2,3,wait,wait
				.Subscribe(x => Ellipsis = new string('.', (int)x));

            disposable.Add(ellipsisDisposable);
#endif

            downloadCts.Token.Register(() => disposable.Dispose());
        }

        public void Report((long ReceivedBytes, long? TotalBytes) progress) => _progressSubject.OnNext(progress);

		public void CancelDownload()
		{
			_downloadCts.Dispose();
		}
	}
}
