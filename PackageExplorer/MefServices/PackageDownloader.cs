using System;
using System.ComponentModel.Composition;
using System.Globalization;
using System.IO;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Threading;
using NuGetPe;
using NuGetPackageExplorer.Types;
using Ookii.Dialogs.Wpf;
using Constants = PackageExplorerViewModel.Constants;
using PackageExplorerViewModel.Types;

namespace PackageExplorer
{
	using HttpClient = System.Net.Http.HttpClient;

	[Export(typeof(INuGetPackageDownloader))]
    internal class PackageDownloader : INuGetPackageDownloader
    {
        private ProgressDialog _progressDialog;
        private readonly object _progressDialogLock = new object();

        [Import]
        public Lazy<MainWindow> MainWindow { get; set; }

        [Import]
        public IUIServices UIServices { get; set; }

		[Import(typeof(ICredentialManager))]
		public ICredentialManager CredentialManager { get; set; }
		
		#region IPackageDownloader Members

		public async Task Download(string targetFilePath, Uri downloadUri, string packageId, string packageVersion)
        {
            var sourceFilePath = await DownloadWithProgress(downloadUri, packageId, packageVersion);
            if (!string.IsNullOrEmpty(sourceFilePath))
            {
                File.Copy(sourceFilePath, targetFilePath, overwrite: true);
            }
        }

        public async Task<ISignaturePackage> Download(Uri downloadUri, string packageId, string packageVersion)
        {
            var tempFilePath = await DownloadWithProgress(downloadUri, packageId, packageVersion);
            return (tempFilePath == null) ? null : new ZipPackage(tempFilePath);
        }

        private async Task<string> DownloadWithProgress(Uri downloadUri, string packageId, string packageVersion)
        {
            var progressDialogText = Resources.Resources.Dialog_DownloadingPackage;
            if (!string.IsNullOrEmpty(packageId))
            {
                progressDialogText = string.Format(CultureInfo.CurrentCulture, progressDialogText, packageId, packageVersion);
            }
            else
            {
                progressDialogText = string.Format(CultureInfo.CurrentCulture, progressDialogText, downloadUri, string.Empty);
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

            try
            {
                var tempFilePath = await DownloadData(downloadUri, OnReportProgress, cts.Token);
                return tempFilePath;
            }
            catch (OperationCanceledException)
            {
                return null;
            }
            catch (Exception exception)
            {
                OnError(exception);
                return null;
            }
            finally
            {
                timer.Stop();

                // close progress dialog when done
                lock (_progressDialogLock)
                {
                    _progressDialog.Close();
                    _progressDialog = null;
                }

                MainWindow.Value.Activate();
            }
        }

        private void OnReportProgress(int percent, string description)
        {
            if (_progressDialog != null)
            {
                // report progress must be done via UI thread
                UIServices.BeginInvoke(() =>
                    {
                        lock (_progressDialogLock)
                        {
                            if (_progressDialog != null)
                            {
                                _progressDialog.ReportProgress(percent, null, description);
                            }
                        }
                    });
            }
        }

        #endregion

        private async Task<string> DownloadData(Uri url, Action<int, string> reportProgressAction, CancellationToken cancelToken)
        {
            var handler = new HttpClientHandler
            {
                Credentials = CredentialManager.Get(url)
            };
            var httpClient = new HttpClient(handler);
            httpClient.DefaultRequestHeaders.UserAgent.ParseAdd(HttpUtility.CreateUserAgentString(Constants.UserAgentClient));

            using (var response = await httpClient.GetAsync(url, HttpCompletionOption.ResponseHeadersRead, cancelToken))
            {
                using (var responseStream = await response.Content.ReadAsStreamAsync())
                {
                    const int chunkSize = 4 * 1024;
                    var totalBytes = (int)(response.Content.Headers.ContentLength ?? 0);
                    var buffer = new byte[chunkSize];
                    var readSoFar = 0;

                    // while reading data from network, we write it to a temp file
                    var tempFilePath = Path.GetTempFileName();
                    using (var fileStream = File.OpenWrite(tempFilePath))
                    {
                        while (readSoFar < totalBytes)
                        {
                            var bytesRead = await responseStream.ReadAsync(buffer, 0, Math.Min(chunkSize, totalBytes - readSoFar), cancelToken);
                            readSoFar += bytesRead;

                            cancelToken.ThrowIfCancellationRequested();

                            fileStream.Write(buffer, 0, bytesRead);
                            OnProgress(readSoFar, totalBytes, reportProgressAction);
                        }
                    }

                    // read all bytes successfully
                    if (readSoFar >= totalBytes)
                    {
                        return tempFilePath;
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
            var percentComplete = (int)((bytesReceived * 100L) / totalBytes);
            var description = string.Format(
                CultureInfo.CurrentCulture,
                "Downloaded {0}KB of {1}KB...",
                ToKB(bytesReceived),
                ToKB(totalBytes));
            reportProgress(percentComplete, description);
        }

        private static long ToKB(long totalBytes)
        {
            return (totalBytes + 1023) / 1024;
        }
    }
}
