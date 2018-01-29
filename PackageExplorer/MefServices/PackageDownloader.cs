using NuGet.Common;
using NuGet.Configuration;
using NuGet.Packaging.Core;
using NuGet.Protocol;
using NuGet.Protocol.Core.Types;
using NuGetPackageExplorer.Types;
using NuGetPe;
using Ookii.Dialogs.Wpf;
using PackageExplorerViewModel;
using System;
using System.ComponentModel.Composition;
using System.Globalization;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Threading;

namespace PackageExplorer
{
    [Export(typeof(INuGetPackageDownloader))]
    internal class PackageDownloader : INuGetPackageDownloader
    {
        private ProgressDialog _progressDialog;
        private readonly object _progressDialogLock = new object();

        [Import]
        public Lazy<MainWindow> MainWindow { get; set; }

        [Import]
        public IUIServices UIServices { get; set; }

        #region IPackageDownloader Members

        public async Task Download(string targetFilePath, SourceRepository sourceRepository, PackageIdentity packageIdentity)
        {
            var sourceFilePath = await DownloadWithProgress(sourceRepository, packageIdentity);
            if (!string.IsNullOrEmpty(sourceFilePath))
            {
                File.Copy(sourceFilePath, targetFilePath, overwrite: true);
            }
        }

        public async Task<ISignaturePackage> Download(SourceRepository sourceRepository, PackageIdentity packageIdentity)
        {
            var tempFilePath = await DownloadWithProgress(sourceRepository, packageIdentity);
            return (tempFilePath == null) ? null : new ZipPackage(tempFilePath);
        }

        private async Task<string> DownloadWithProgress(SourceRepository sourceRepository, PackageIdentity packageIdentity)
        {
            var progressDialogText = Resources.Resources.Dialog_DownloadingPackage;
            if (packageIdentity.HasVersion)
            {
                progressDialogText = string.Format(CultureInfo.CurrentCulture, progressDialogText, packageIdentity.Id, packageIdentity.Version);
            }
            else
            {
                progressDialogText = string.Format(CultureInfo.CurrentCulture, progressDialogText, packageIdentity.Id, string.Empty);
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
                var httpProgressProvider = new ProgressHttpHandlerResourceV3Provider(OnProgress);
                var additionalProviders = new[] { new Lazy<INuGetResourceProvider>(() => httpProgressProvider) };

                var repository = PackageRepositoryFactory.CreateRepository(sourceRepository.PackageSource, additionalProviders);
                var downloadResource = await repository.GetResourceAsync<DownloadResource>(cts.Token);

                var context = new PackageDownloadContext(new SourceCacheContext(), Path.GetTempPath(), true);

                using (var result = await downloadResource.GetDownloadResourceResultAsync(packageIdentity, context, string.Empty, NullLogger.Instance, cts.Token))
                {
                    if (result.Status == DownloadResourceResultStatus.Cancelled)
                    {
                        throw new OperationCanceledException();
                    }
                    if (result.Status == DownloadResourceResultStatus.NotFound)
                    {
                        throw new Exception(string.Format("Package '{0} {1}' not found", packageIdentity.Id, packageIdentity.Version));
                    }

                    var tempFilePath = Path.GetTempFileName();

                    using (var fileStream = File.OpenWrite(tempFilePath))
                    {
                        await result.PackageStream.CopyToAsync(fileStream);
                    }

                    return tempFilePath;
                }
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

        private void OnError(Exception error)
        {
            UIServices.Show((error.InnerException ?? error).Message, MessageLevel.Error);
        }

        private void OnProgress(int bytesReceived, int totalBytes)
        {
            var percentComplete = (int)((bytesReceived * 100L) / totalBytes);
            var description = string.Format(
                CultureInfo.CurrentCulture,
                "Downloaded {0}KB of {1}KB...",
                ToKB(bytesReceived),
                ToKB(totalBytes));
            OnReportProgress(percentComplete, description);
        }

        private static long ToKB(long totalBytes)
        {
            return (totalBytes + 1023) / 1024;
        }
    }

    // helper classes for getting http progress events
    internal class ProgressHttpMessageHandler : DelegatingHandler
    {
        private readonly Action<int, int> _progressAction;

        public ProgressHttpMessageHandler(HttpClientHandler handler, Action<int, int> progressAction) : base(handler)
        {
            _progressAction = progressAction;
        }

        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            var response = await base.SendAsync(request, cancellationToken);

            if (IsBinaryMediaType(response.Content.Headers.ContentType.MediaType))
            {
                var totalSize = response.Content.Headers.ContentLength;

                if (totalSize.HasValue)
                {
                    var innerStream = await response.Content.ReadAsStreamAsync();

                    response.Content = new StreamContent(new ProgressStream(innerStream, size => _progressAction(size, (int)totalSize.Value)));
                }
            }

            return response;
        }

        private static bool IsBinaryMediaType(string mediaType)
        {
            return mediaType == "application/octet-stream" || // NuGet Protocol v3
                mediaType == "binary/octet-stream"; // NuGet Protocol v2
        }
    }

    internal class ProgressStream : Stream
    {
        private readonly Stream _inner;
        private readonly Action<int> _progress;

        private int _length;

        public ProgressStream(Stream inner, Action<int> progress)
        {
            _inner = inner;
            _progress = progress;
        }

        public override bool CanRead => _inner.CanRead;

        public override bool CanSeek => _inner.CanSeek;

        public override bool CanWrite => _inner.CanWrite;

        public override long Length => _inner.Length;

        public override long Position { get => _inner.Position; set => _inner.Position = value; }

        public override void Flush() => _inner.Flush();

        public override int Read(byte[] buffer, int offset, int count)
        {
            var result = _inner.Read(buffer, offset, count);

            if (result > 0)
            {
                _length += result;

                _progress(_length);
            }

            return result;
        }

        public override long Seek(long offset, SeekOrigin origin) => _inner.Seek(offset, origin);

        public override void SetLength(long value) => _inner.SetLength(value);

        public override void Write(byte[] buffer, int offset, int count) => _inner.Write(buffer, offset, count);

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                _inner.Dispose();
            }

            base.Dispose(disposing);
        }
    }

    internal class ProgressHttpHandlerResourceV3Provider : ResourceProvider
    {
        private readonly Action<int, int> _progressAction;

        public ProgressHttpHandlerResourceV3Provider(Action<int, int> progressAction)
            : base(typeof(HttpHandlerResource),
                  nameof(ProgressHttpHandlerResourceV3Provider),
                  NuGetResourceProviderPositions.First)
        {
            _progressAction = progressAction;
        }

        public override Task<Tuple<bool, INuGetResource>> TryCreate(SourceRepository source, CancellationToken token)
        {
            HttpHandlerResourceV3 curResource = null;

            if (source.PackageSource.IsHttp)
            {
                curResource = CreateResource(source.PackageSource);
            }

            return Task.FromResult(new Tuple<bool, INuGetResource>(curResource != null, curResource));
        }

        private HttpHandlerResourceV3 CreateResource(PackageSource packageSource)
        {
            var sourceUri = packageSource.SourceUri;
            var proxy = ProxyCache.Instance.GetProxy(sourceUri);

            // replace the handler with the proxy aware handler
            var clientHandler = new HttpClientHandler
            {
                Proxy = proxy,
                AutomaticDecompression = (DecompressionMethods.GZip | DecompressionMethods.Deflate)
            };

            // HTTP handler pipeline can be injected here, around the client handler
            HttpMessageHandler messageHandler = new ProgressHttpMessageHandler(clientHandler, _progressAction);

            if (proxy != null)
            {
                messageHandler = new ProxyAuthenticationHandler(clientHandler, HttpHandlerResourceV3.CredentialService, ProxyCache.Instance);
            }

            messageHandler = new HttpSourceAuthenticationHandler(packageSource, clientHandler, HttpHandlerResourceV3.CredentialService)
            {
                InnerHandler = messageHandler
            };

            var resource = new HttpHandlerResourceV3(clientHandler, messageHandler);

            return resource;
        }
    }
}
