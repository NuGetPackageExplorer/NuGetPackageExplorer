using System;
using System.ComponentModel.Composition;
using System.IO;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Threading;
using NuGet;
using NuGetPackageExplorer.Types;
using Ookii.Dialogs.Wpf;

namespace PackageExplorer {

    [Export(typeof(IPackageDownloader))]
    internal class PackageDownloader : IPackageDownloader {
        [Import]
        public Lazy<MainWindow> MainWindow { get; set; }

        [Import]
        public IUIServices UIServices { get; set; }

        private ProgressDialog _progressDialog;

        public void Download(
            Uri downloadUri,
            string packageId,
            Version packageVersion,
            Action<IPackage> callback) {

            _progressDialog = new ProgressDialog {
                Text = "Downloading package " + packageId + " " + packageVersion.ToString(),
                WindowTitle = Resources.Resources.Dialog_Title,
                ShowTimeRemaining = true,
                CancellationText = "Canceling download..."
            };
            _progressDialog.ShowDialog(MainWindow.Value);

            // polling for Cancel button being clicked
            CancellationTokenSource cts = new CancellationTokenSource();
            var timer = new DispatcherTimer() {
                Interval = TimeSpan.FromMilliseconds(200)
            };
            timer.Tick += (o, e) => {
                if (_progressDialog.CancellationPending) {
                    timer.Stop();
                    cts.Cancel();
                }
            };
            timer.Start();

            // report progress must be done via UI thread
            Action<int, string> reportProgress = (percent, description) => {
                UIServices.BeginInvoke(() => _progressDialog.ReportProgress(percent, null, description));
            };

            // download package on background thread
            TaskScheduler uiScheduler = TaskScheduler.FromCurrentSynchronizationContext();
            Task.Factory.StartNew(
                () => DownloadData(downloadUri, reportProgress, cts.Token),
                cts.Token
            ).ContinueWith(
                task => {
                    timer.Stop();

                    // close progress dialog when done
                    _progressDialog.Close();
                    _progressDialog = null;
                    MainWindow.Value.Activate();

                    if (task.Exception != null) {
                        OnError(task.Exception);
                    }
                    else if (!task.IsCanceled) {
                        IPackage package = task.Result;
                        callback(package);
                    }
                },
                uiScheduler
            );
        }

        private IPackage DownloadData(Uri url, Action<int, string> reportProgressAction, CancellationToken cancelToken) {

            var httpClient = new RedirectedHttpClient(url) {
                UserAgent = HttpUtility.CreateUserAgentString(PackageExplorerViewModel.Constants.UserAgentClient),
                AcceptCompression = false
            };

            using (HttpWebResponse response = (HttpWebResponse)httpClient.GetResponse()) {
                cancelToken.ThrowIfCancellationRequested();
                using (Stream requestStream = response.GetResponseStream()) {
                    int chunkSize = 4 * 1024;
                    int totalBytes = (int)response.ContentLength;
                    byte[] buffer = new byte[chunkSize];
                    int readSoFar = 0;

                    // while reading data from network, we write it to a temp file
                    string tempFilePath = Path.GetTempFileName();
                    using (FileStream fileStream = File.OpenWrite(tempFilePath)) {
                        while (readSoFar < totalBytes) {
                            int bytesRead = requestStream.Read(buffer, 0, Math.Min(chunkSize, totalBytes - readSoFar));
                            readSoFar += bytesRead;

                            cancelToken.ThrowIfCancellationRequested();

                            fileStream.Write(buffer, 0, bytesRead);
                            OnProgress(readSoFar, totalBytes, reportProgressAction);
                        }
                    }

                    // read all bytes successfully
                    if (readSoFar >= totalBytes) {
                        return new ZipPackage(tempFilePath);
                    }
                }
            }
            return null;
        }

        private void OnError(Exception error) {
            UIServices.Show((error.InnerException ?? error).Message, MessageLevel.Error);
        }

        private void OnProgress(int bytesReceived, int totalBytes, Action<int, string> reportProgress) {
            int percentComplete = (bytesReceived * 100) / totalBytes;
            string description = String.Format("Downloaded {0}KB of {1}KB...", ToKB(bytesReceived).ToString(), ToKB(totalBytes).ToString());
            reportProgress(percentComplete, description);
        }

        private long ToKB(long totalBytes) {
            return (totalBytes + 1023) / 1024;
        }
    }
}