using System;
using System.ComponentModel;
using System.ComponentModel.Composition;
using System.Globalization;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using NuGet.Common;
using NuGet.Configuration;
using NuGet.Packaging.Core;
using NuGet.Protocol;
using NuGet.Protocol.Core.Types;

using NuGetPackageExplorer.Types;
using NuGetPe;

using PackageExplorerViewModel;

#if !HAS_UNO && !USE_WINUI
using Ookii.Dialogs.Wpf;
#endif

#if __WASM__
using NupkgExplorer.Client;
#endif

namespace PackageExplorer
{
    [Export(typeof(INuGetPackageDownloader))]
    internal class PackageDownloader : INuGetPackageDownloader
    {
        private static readonly FileSizeConverter FileSizeConverter = new();

#pragma warning disable CS8618 // Non-nullable field is uninitialized.
        [Import]
        public Lazy<MainWindow> MainWindow { get; set; }

        [Import]
        public IUIServices UIServices { get; set; }

#if __WASM__
        [Import]
        public INugetEndpoint NugetEndpoint { get; set; }
#endif
#pragma warning restore CS8618 // Non-nullable field is uninitialized.

        #region IPackageDownloader Members

        public async Task Download(string targetFilePath, SourceRepository sourceRepository, PackageIdentity packageIdentity)
        {
            var sourceFilePath = await DownloadWithProgress(sourceRepository, packageIdentity);
            if (!string.IsNullOrEmpty(sourceFilePath))
            {
                File.Copy(sourceFilePath, targetFilePath, overwrite: true);
            }
        }

        public async Task<ISignaturePackage?> Download(SourceRepository sourceRepository, PackageIdentity packageIdentity)
        {
            var tempFilePath = await DownloadWithProgress(sourceRepository, packageIdentity);
            try
            {
                return (tempFilePath == null) ? null : new ZipPackage(tempFilePath);
            }
            catch (Exception e)
            {
                // Don't send telemetry error for bad package
                if (!(e is InvalidDataException))
                {
                    DiagnosticsClient.TrackException(e);
                }

                UIServices.Show(e.Message, MessageLevel.Error);
                return null;
            }

        }

        private Task<string?> DownloadWithProgress(SourceRepository sourceRepository, PackageIdentity packageIdentity)
        {
#if __WASM__
            // FIXME#14: we are bypassing the entire implementation, because DownloadResource could not be created on WASM (but works skia)
            return NugetEndpoint
                .DownloadPackage(packageIdentity.Id, packageIdentity.Version.ToNormalizedString())
                .ContinueWith(x =>
                {
                    var path = $"./tmp/{Guid.NewGuid()}.nupkg";
                    Directory.CreateDirectory(Path.GetDirectoryName(path));
                    using (var file = File.OpenWrite(path))
                    {
                        x.Result.CopyTo(file);
                    }

                    return path;
                });
#endif

#if HAS_UNO || USE_WINUI
            string? description = null;
            int? percent = null;
            var updated = 0;

            var tcs = new TaskCompletionSource<string?>();
            var cts = new CancellationTokenSource();

            // TODO: progress/error reporting & cancellation
            DoWorkAsync().ContinueWith(x => tcs.TrySetResult(x.Result));
#else
            var progressDialogText = Resources.Dialog_DownloadingPackage;
            if (packageIdentity.HasVersion)
            {
                progressDialogText = string.Format(CultureInfo.CurrentCulture, progressDialogText, packageIdentity.Id, packageIdentity.Version);
            }
            else
            {
                progressDialogText = string.Format(CultureInfo.CurrentCulture, progressDialogText, packageIdentity.Id, string.Empty);
            }

            string? description = null;
            int? percent = null;
            var updated = 0;

            var progressDialogLock = new object();
#pragma warning disable CA2000 // Dispose objects before losing scope (handled in finally below)
            var progressDialog = new ProgressDialog
            {
                Text = progressDialogText,
                WindowTitle = Resources.Dialog_Title,
                ShowTimeRemaining = true,
                CancellationText = "Canceling download..."
            };

            // polling for Cancel button being clicked
            var cts = new CancellationTokenSource();
            var timer = new System.Timers.Timer(100);
            var tcs = new TaskCompletionSource<string?>();

#pragma warning restore CA2000 // Dispose objects before losing scope

            timer.Elapsed += (o, e) =>
                          {
                              lock (progressDialogLock)
                              {
                                  if (progressDialog.CancellationPending)
                                  {
                                      timer.Stop();
                                      cts.Cancel();
                                  }
                                  else if (Interlocked.CompareExchange(ref updated, 0, 1) == 1)
                                  {
                                      progressDialog.ReportProgress(percent.GetValueOrDefault(), null, description);
                                  }
                              }
                          };


            progressDialog.DoWork += (object? sender, DoWorkEventArgs args) =>
            {
                var t = DoWorkAsync();
                t.Wait(cts.Token);
                tcs.TrySetResult(t.Result);
            };
            progressDialog.RunWorkerCompleted += (object? sender, RunWorkerCompletedEventArgs args) =>
            {
                MainWindow.Value.Activate();
            };

            progressDialog.ShowDialog(MainWindow.Value);

            timer.Start();
#endif


            async Task<string?> DoWorkAsync()
            {
                try
                {
                    var httpProgressProvider = new ProgressHttpHandlerResourceV3Provider(OnProgress);
                    var repository = PackageRepositoryFactory.CreateRepository(sourceRepository.PackageSource, new[] { new Lazy<INuGetResourceProvider>(() => httpProgressProvider) });
                    var downloadResource = await repository.GetResourceAsync<DownloadResource>(cts!.Token).ConfigureAwait(false);

                    using var sourceCacheContext = new SourceCacheContext() { NoCache = true };
                    var context = new PackageDownloadContext(sourceCacheContext, Path.GetTempPath(), true);

                    using var result = await downloadResource.GetDownloadResourceResultAsync(packageIdentity, context, string.Empty, NullLogger.Instance, cts.Token).ConfigureAwait(false);
                    if (result.Status == DownloadResourceResultStatus.Cancelled)
                        throw new OperationCanceledException();

                    if (result.Status == DownloadResourceResultStatus.NotFound)
                        throw new PackageNotFoundException($"Package '{packageIdentity.Id} {packageIdentity.Version}' not found");

                    var tempFilePath = Path.GetTempFileName();
                    using (var fileStream = File.OpenWrite(tempFilePath))
                    {
                        await result.PackageStream.CopyToAsync(fileStream).ConfigureAwait(false);
                    }

                    return tempFilePath;
                }
#if !HAS_UNO && !USE_WINUI
                catch (OperationCanceledException)
                {
                    return null;
                }
                catch (Exception exception)
                {
                    OnError(exception);
                    return null;
                }
#endif
                finally
                {
#if HAS_UNO || USE_WINUI
                    cts!.Dispose();
#else
                    timer!.Stop();
                    timer.Dispose();
                    cts!.Dispose();

                    // close progress dialog when done
                    lock (progressDialogLock!)
                    {
                        progressDialog!.Dispose();
                    }
#endif
                }
            }


            void OnProgress(long bytesReceived, long? totalBytes)
            {
#if HAS_UNO
                var currentCulture = CultureInfo.CurrentCulture.ToString();
#else
                var currentCulture = CultureInfo.CurrentCulture;
#endif

                if (totalBytes.HasValue)
                {
                    // TODO: remove ! once https://github.com/dotnet/roslyn/issues/33330 is fixed
                    percent = (int)((bytesReceived * 100L) / totalBytes)!;
                    description = string.Format(
                       CultureInfo.CurrentCulture,
                       "Downloaded {0} of {1}...",
                       FileSizeConverter.Convert(bytesReceived, typeof(string), null, currentCulture),
                       FileSizeConverter.Convert(totalBytes.Value, typeof(string), null, currentCulture));
                }
                else
                {
                    percent = null;
                    description = string.Format(
                        CultureInfo.CurrentCulture,
                        "Downloaded {0}...",
                        FileSizeConverter.Convert(bytesReceived, typeof(string), null, currentCulture));
                }
                Interlocked.Exchange(ref updated, 1);
            }

            return tcs.Task;
        }

#endregion

        private void OnError(Exception error)
        {
            UIServices.Show((error.InnerException ?? error).Message, MessageLevel.Error);
        }
    }

    // helper classes for getting http progress events
    internal class ProgressHttpMessageHandler : DelegatingHandler
    {
        private readonly Action<long, long?> _progressAction;

        public ProgressHttpMessageHandler(HttpClientHandler handler, Action<long, long?> progressAction) : base(handler)
        {
            _progressAction = progressAction;
        }

        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            var response = await base.SendAsync(request, cancellationToken);

            if (IsBinaryMediaType(response.Content?.Headers.ContentType?.MediaType))
            {
                var totalSize = response.Content!.Headers.ContentLength;
                var innerStream = await response.Content.ReadAsStreamAsync(cancellationToken);

                response.Content = new StreamContent(new ProgressStream(innerStream, size => _progressAction(size, totalSize)));
            }

            return response;
        }

        private static bool IsBinaryMediaType(string? mediaType)
        {
            return mediaType == "application/octet-stream" || // NuGet Protocol v3
                mediaType == "binary/octet-stream"; // NuGet Protocol v2
        }
    }


    [Serializable]
    public class PackageNotFoundException : Exception
    {
        public PackageNotFoundException() { }
        public PackageNotFoundException(string message) : base(message) { }
        public PackageNotFoundException(string message, Exception inner) : base(message, inner) { }
        protected PackageNotFoundException(
          System.Runtime.Serialization.SerializationInfo info,
          System.Runtime.Serialization.StreamingContext context) : base(info, context) { }
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

        public override async Task<int> ReadAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken)
        {
            var result = await _inner.ReadAsync(buffer.AsMemory(offset, count), cancellationToken).ConfigureAwait(false);

            if (result > 0)
            {
                _length += result;

                _progress(_length);
            }

            return result;
        }

        public override async ValueTask<int> ReadAsync(Memory<byte> buffer, CancellationToken cancellationToken = default)
        {
            var result = await _inner.ReadAsync(buffer, cancellationToken);

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

        public override Task<Tuple<bool, INuGetResource?>> TryCreate(SourceRepository source, CancellationToken token)
        {
            HttpHandlerResourceV3? curResource = null;

            if (source.PackageSource.IsHttp)
            {
                curResource = CreateResource(source.PackageSource);
            }

            return Task.FromResult(new Tuple<bool, INuGetResource?>(curResource != null, curResource));
        }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Reliability", "CA2000:Dispose objects before losing scope", Justification = "<Pending>")]
        private HttpHandlerResourceV3 CreateResource(PackageSource packageSource)
        {
            var sourceUri = packageSource.SourceUri;
            var proxy = ProxyCache.Instance.GetProxy(sourceUri);

            // replace the handler with the proxy aware handler
            var clientHandler = new HttpClientHandler
            {
#if !HAS_UNO
                Proxy = proxy,
                AutomaticDecompression = (DecompressionMethods.GZip | DecompressionMethods.Deflate)
#endif
            };

            // HTTP handler pipeline can be injected here, around the client handler
            HttpMessageHandler messageHandler = new ProgressHttpMessageHandler(clientHandler, _progressAction);

            if (proxy != null)
            {
                messageHandler = new ProxyAuthenticationHandler(clientHandler, HttpHandlerResourceV3.CredentialService.Value, ProxyCache.Instance);
            }

            {
                //var innerHandler = messageHandler;

                // TODO: Investigate what changed in this type
                //    messageHandler = new StsAuthenticationHandler(packageSource, TokenStore.Instance)
                //    {
                //        InnerHandler = innerHandler
                //    };
            }
#if !__WASM__ // HttpSourceAuthenticationHandler will no matter how set the credentials which isnt supported on wasm
            {
                var innerHandler = messageHandler;

                messageHandler = new HttpSourceAuthenticationHandler(packageSource, clientHandler, HttpHandlerResourceV3.CredentialService.Value)
                {
                    InnerHandler = innerHandler
                };
            }
#endif

            var resource = new HttpHandlerResourceV3(clientHandler, messageHandler);

            return resource;
        }
    }
}
