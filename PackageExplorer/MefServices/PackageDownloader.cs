﻿using NuGet.Common;
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
        private static readonly FileSizeConverter FileSizeConverter = new FileSizeConverter();

        private ProgressDialog _progressDialog;
        private string _lastDescription;
        private long? _lastPecent;
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

            // polling for Cancel button being clicked
            var cts = new CancellationTokenSource();
            var timer = new DispatcherTimer
            {
                Interval = TimeSpan.FromMilliseconds(200)
            };

            timer.Tick += (o, e) =>
                          {
                              lock (_progressDialogLock)
                              {
                                  if (_progressDialog.CancellationPending)
                                  {
                                      timer.Stop();
                                      cts.Cancel();
                                  }
                                  else if (_progressDialog.Description != _lastDescription)
                                  {
                                      if (!_progressDialog.IsOpen)
                                      {
                                          _progressDialog.ProgressBarStyle = _lastPecent.HasValue ? ProgressBarStyle.ProgressBar : ProgressBarStyle.MarqueeProgressBar;
                                          _progressDialog.ShowDialog(MainWindow.Value);
                                      }
                                      _progressDialog.ReportProgress((int)_lastPecent.GetValueOrDefault(), null, _lastDescription);
                                  }
                              }
                          };
            timer.Start();

            try
            {
                var httpProgressProvider = new ProgressHttpHandlerResourceV3Provider(OnProgress);
                var additionalProviders = new[] { new Lazy<INuGetResourceProvider>(() => httpProgressProvider) };

                var repository = PackageRepositoryFactory.CreateRepository(sourceRepository.PackageSource, additionalProviders);
                var downloadResource = await repository.GetResourceAsync<DownloadResource>(cts.Token);

                using (var sourceCacheContext = new SourceCacheContext())
                {
                    var context = new PackageDownloadContext(sourceCacheContext, Path.GetTempPath(), true);

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

        #endregion

        private void OnError(Exception error)
        {
            UIServices.Show((error.InnerException ?? error).Message, MessageLevel.Error);
        }

        private void OnProgress(long bytesReceived, long? totalBytes)
        {
            if (totalBytes.HasValue)
            {
                _lastPecent = (int)((bytesReceived * 100L) / totalBytes);
                _lastDescription = string.Format(
                   CultureInfo.CurrentCulture,
                   "Downloaded {0} of {1}...",
                   FileSizeConverter.Convert(bytesReceived, typeof(string), null, CultureInfo.CurrentCulture),
                   FileSizeConverter.Convert(totalBytes.Value, typeof(string), null, CultureInfo.CurrentCulture));
            }
            else
            {
                _lastPecent = totalBytes;
                _lastDescription = string.Format(
                        CultureInfo.CurrentCulture,
                        "Downloaded {0}...",
                        FileSizeConverter.Convert(bytesReceived, typeof(string), null, CultureInfo.CurrentCulture));
            }
        }
    }

    // helper classes for getting http progress events
    internal class ProgressHttpMessageHandler : DelegatingHandler
    {
        private Action<long, long?> _progressAction;

        public ProgressHttpMessageHandler(HttpClientHandler handler, Action<long, long?> progressAction) : base(handler)
        {
            _progressAction = progressAction;
        }

        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            var response = await base.SendAsync(request, cancellationToken);

            if (IsBinaryMediaType(response.Content.Headers.ContentType.MediaType))
            {
                var totalSize = response.Content.Headers.ContentLength;
                var innerStream = await response.Content.ReadAsStreamAsync();

                response.Content = new StreamContent(new ProgressStream(innerStream, size => _progressAction(size, totalSize)));
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
        private readonly Action<long> _progress;

        private long _length;

        public ProgressStream(Stream inner, Action<long> progress)
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

    // https://github.com/NuGet/NuGet.Client/blob/5244dc7596f0cc0ed65984dc8c040d23b0e9c09b/src/NuGet.Core/NuGet.Protocol/HttpSource/HttpHandlerResourceV3Provider.cs
    internal class ProgressHttpHandlerResourceV3Provider : ResourceProvider
    {
        private readonly Action<long, long?> _progressAction;

        public ProgressHttpHandlerResourceV3Provider(Action<long, long?> progressAction)
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

            {
                var innerHandler = messageHandler;

                messageHandler = new StsAuthenticationHandler(packageSource, TokenStore.Instance)
                {
                    InnerHandler = innerHandler
                };
            }
            {
                var innerHandler = messageHandler;

                messageHandler = new HttpSourceAuthenticationHandler(packageSource, clientHandler, HttpHandlerResourceV3.CredentialService)
                {
                    InnerHandler = innerHandler
                };
            }

            var resource = new HttpHandlerResourceV3(clientHandler, messageHandler);

            return resource;
        }
    }
}
