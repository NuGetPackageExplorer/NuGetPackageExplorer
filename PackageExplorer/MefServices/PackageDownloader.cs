using System;
using System.ComponentModel.Composition;
using System.Globalization;
using System.IO;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Threading;
using NuGet;
using NuGetPackageExplorer.Types;
using Ookii.Dialogs.Wpf;
using Constants = PackageExplorerViewModel.Constants;

namespace PackageExplorer
{
    [Export(typeof(IPackageDownloader))]
    internal class PackageDownloader : IPackageDownloader
    {
        private ProgressDialog _progressDialog;

        [Import]
        public Lazy<MainWindow> MainWindow { get; set; }

        [Import]
        public IUIServices UIServices { get; set; }

        #region IPackageDownloader Members

        public void Download(Uri downloadUri, string packageId, SemanticVersion packageVersion, Action<IPackage> callback)
        {
            string progressDialogText = Resources.Resources.Dialog_DownloadingPackage;
            if (!string.IsNullOrEmpty(packageId))
            {
                progressDialogText = String.Format(CultureInfo.CurrentCulture, progressDialogText, packageId,
                                                   packageVersion);
            }
            else
            {
                progressDialogText = String.Format(CultureInfo.CurrentCulture, progressDialogText, downloadUri,
                                                   String.Empty);
            }

            _progressDialog = new ProgressDialog
                              {
                                  Text = progressDialogText,
                                  WindowTitle = Resources.Resources.Dialog_Title,
                                  ShowTimeRemaining = true,
                                  CancellationText = "Canceling download..."
                              };
            _progressDialog.ShowDialog(MainWindow.Value);

            // polling for Cancel button being clicked
            var cts = new CancellationTokenSource();
            var timer = new DispatcherTimer
                        {
                            Interval = TimeSpan.FromMilliseconds(200)
                        };
            timer.Tick += (o, e) =>
                          {
                              if (_progressDialog.CancellationPending)
                              {
                                  timer.Stop();
                                  cts.Cancel();
                              }
                          };
            timer.Start();

            // report progress must be done via UI thread
            Action<int, string> reportProgress =
                (percent, description) => { UIServices.BeginInvoke(() => _progressDialog.ReportProgress(percent, null, description)); };

            // download package on background thread
            TaskScheduler uiScheduler = TaskScheduler.FromCurrentSynchronizationContext();
            Task.Factory.StartNew(
                () => DownloadData(downloadUri, reportProgress, cts.Token),
                cts.Token
                ).ContinueWith(
                    task =>
                    {
                        timer.Stop();

                        // close progress dialog when done
                        _progressDialog.Close();
                        _progressDialog = null;
                        MainWindow.Value.Activate();

                        if (task.Exception != null)
                        {
                            OnError(task.Exception);
                        }
                        else if (!task.IsCanceled)
                        {
                            IPackage package = task.Result;
                            callback(package);
                        }
                    },
                    uiScheduler
                );
        }

        #endregion

        private IPackage DownloadData(Uri url, Action<int, string> reportProgressAction, CancellationToken cancelToken)
        {
            var httpClient = new RedirectedHttpClient(url)
                             {
                                 UserAgent =
                                     HttpUtility.CreateUserAgentString(
                                         Constants.UserAgentClient),
                                 AcceptCompression = false
                             };

            using (var response = (HttpWebResponse) httpClient.GetResponse())
            {
                cancelToken.ThrowIfCancellationRequested();
                using (Stream requestStream = response.GetResponseStream())
                {
                    int chunkSize = 4*1024;
                    var totalBytes = (int) response.ContentLength;
                    var buffer = new byte[chunkSize];
                    int readSoFar = 0;

                    // while reading data from network, we write it to a temp file
                    string tempFilePath = Path.GetTempFileName();
                    using (FileStream fileStream = File.OpenWrite(tempFilePath))
                    {
                        while (readSoFar < totalBytes)
                        {
                            int bytesRead = requestStream.Read(buffer, 0, Math.Min(chunkSize, totalBytes - readSoFar));
                            readSoFar += bytesRead;

                            cancelToken.ThrowIfCancellationRequested();

                            fileStream.Write(buffer, 0, bytesRead);
                            OnProgress(readSoFar, totalBytes, reportProgressAction);
                        }
                    }

                    // read all bytes successfully
                    if (readSoFar >= totalBytes)
                    {
                        return new ZipPackage(tempFilePath);
                    }
                }
            }
            return null;
        }

        private void OnError(Exception error)
        {
            UIServices.Show((error.InnerException ?? error).Message, MessageLevel.Error);
        }

        private void OnProgress(int bytesReceived, int totalBytes, Action<int, string> reportProgress)
        {
            int percentComplete = (bytesReceived*100)/totalBytes;
            string description = String.Format("Downloaded {0}KB of {1}KB...", ToKB(bytesReceived), ToKB(totalBytes));
            reportProgress(percentComplete, description);
        }

        private long ToKB(long totalBytes)
        {
            return (totalBytes + 1023)/1024;
        }
    }
}