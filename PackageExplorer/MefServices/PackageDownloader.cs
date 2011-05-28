using System;
using System.ComponentModel.Composition;
using System.IO;
using System.Net;
using System.Windows.Threading;
using NuGet;
using Ookii.Dialogs.Wpf;
using NuGetPackageExplorer.Types;

namespace PackageExplorer {

    [Export(typeof(IPackageDownloader))]
    internal class PackageDownloader : IPackageDownloader {

        [Import]
        public Lazy<MainWindow> MainWindow { get; set; }

        [Import]
        public IUIServices UIServices { get; set; }

        public void Download(
            Uri downloadUri, 
            string packageId, 
            Version packageVersion, 
            IProxyService proxyService,
            Action<IPackage> callback) {

            var progressDialog = new ProgressDialog {
                Text = "Downloading package " + packageId + " " + packageVersion.ToString(),
                WindowTitle = Resources.Resources.Dialog_Title,
                ShowTimeRemaining = true,
                CancellationText = "Canceling download..."
            };
            progressDialog.ShowDialog(MainWindow.Value);

            DownloadData(downloadUri, proxyService, progressDialog, callback);
        }

        private void DownloadData(Uri uri, IProxyService proxyService, ProgressDialog progressDialog, Action<IPackage> callback) {
            WebClient client = new WebClient();
            string userAgent = HttpUtility.CreateUserAgentString(PackageExplorerViewModel.Constants.UserAgentClient);
            client.Headers[HttpRequestHeader.UserAgent] = userAgent;
            client.UseDefaultCredentials = true;
            client.Proxy = proxyService.GetProxy(uri);

            client.DownloadDataCompleted += (sender, e) => {
                // close the progress dialog first thing
                progressDialog.Close();
                // when the progress dialog close, the main window loses focus unexpectedly. 
                // call Activate() so that the main window retains focus
                MainWindow.Value.Activate();

                if (!e.Cancelled) {
                    // the progress takes a while to disappear, wait for a bit 
                    // before we load the package.
                    DispatcherTimer timer = new DispatcherTimer() {
                        Interval = TimeSpan.FromMilliseconds(500)
                    };
                    timer.Tick += (s, args) => {
                        timer.Stop();
                        OnCompleted(callback, e);
                    };

                    timer.Start();
                }
            };

            client.DownloadProgressChanged += (sender, e) => {
                // detect when user presses Cancel button
                if (progressDialog.CancellationPending) {
                    client.CancelAsync();
                }
                else {
                    OnProgress(progressDialog, e.ProgressPercentage, e.BytesReceived, e.TotalBytesToReceive);
                }
            };

            client.DownloadDataAsync(uri);
        }

        private void OnCompleted(Action<IPackage> callback, DownloadDataCompletedEventArgs e) {
            if (e.Error != null) {
                OnError(e.Error);
            }
            else {
                string tempFilePath = SaveResultToTempFile(e.Result);
                var package = new ZipPackage(tempFilePath);
                callback(package);
            }
        }

        private string SaveResultToTempFile(byte[] bytes) {
            string tempFile = Path.GetTempFileName();
            File.WriteAllBytes(tempFile, bytes);
            return tempFile;
        }

        public void OnError(Exception error) {
            UIServices.Show((error.InnerException ?? error).Message, MessageLevel.Error);
        }

        private void OnProgress(ProgressDialog progressDialog, int percentComplete, long bytesReceived, long totalBytes) {
            if (percentComplete < 0) {
                percentComplete = 0;
            }
            if (percentComplete > 100) {
                percentComplete = 100;
            }

            var description = String.Format("Downloaded {0}KB of {1}KB...", ToKB(bytesReceived).ToString(), ToKB(totalBytes).ToString());
            progressDialog.ReportProgress(percentComplete, null, description);
        }

        private long ToKB(long totalBytes) {
            return (totalBytes + 1023) / 1024;
        }
    }
}
