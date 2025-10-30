using System.Reactive.Linq;
using System.Reactive.Subjects;

using NupkgExplorer.Framework.MVVM;

using Uno.Disposables;

namespace NupkgExplorer.Presentation.Dialogs
{
    public partial class DownloadProgressDialogViewModel : ViewModelBase, IProgress<(long ReceivedBytes, long? TotalBytes)>
    {
        private readonly ReplaySubject<(long ReceivedBytes, long? TotalBytes)> _progressSubject;
        private readonly CancellationDisposable _downloadCts;

        public string? PackageName
        {
            get => GetProperty<string>();
            set => SetProperty(value);
        }

        public string? PackageVersion
        {
            get => GetProperty<string>();
            set => SetProperty(value);
        }

        public string? Ellipsis
        {
            get => GetProperty<string>();
            set => SetProperty(value);
        }

        public double? Progress
        {
            get => GetProperty<double?>();
            set => SetProperty(value);
        }

        public string? ReceivedTest
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
            ArgumentNullException.ThrowIfNull(downloadCts);

            _progressSubject = new ReplaySubject<(long ReceivedBytes, long? TotalBytes)>(1);
            _downloadCts = downloadCts;
            PackageName = packageName;
            PackageVersion = packageVersion;

#pragma warning disable CA2000 // Dispose objects before losing scope
            var disposable = new CompositeDisposable();
#pragma warning restore CA2000 // Dispose objects before losing scope

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
