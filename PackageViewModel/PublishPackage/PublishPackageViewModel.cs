using System;
using System.IO;
using System.Net;
using NuGet;

namespace PackageExplorerViewModel {
    public class PublishPackageViewModel : ViewModelBase, IObserver<int> {
        private readonly IPackageMetadata _package;
        private readonly Lazy<Stream> _packageStream;
        private readonly string _publishUrl;
        private readonly IProxyService _proxyService;

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Design", "CA1054:UriParametersShouldNotBeStrings", MessageId = "0#")]
        public PublishPackageViewModel(string publishUrl, PackageViewModel viewModel) {
            if (viewModel == null) {
                throw new ArgumentNullException("viewModel");
            }
            _publishUrl = publishUrl;
            _package = viewModel.PackageMetadata;
            _packageStream = new Lazy<Stream>(viewModel.GetCurrentPackageStream);
            _proxyService = viewModel.ProxyService;
        }

        private string _publishKey;

        public string PublishKey {
            get { return _publishKey; }
            set {
                if (_publishKey != value) {
                    _publishKey = value;
                    OnPropertyChanged("PublishKey");
                }
            }
        }

        private bool? _pushOnly = false;

        public bool? PushOnly {
            get {
                return _pushOnly;
            }
            set {
                if (_pushOnly != value) {
                    _pushOnly = value;
                    OnPropertyChanged("PushOnly");
                }
            }
        }

        public string Id {
            get { return _package.Id; }
        }

        public string Version {
            get { return _package.Version.ToString(); }
        }

        private bool _hasError;

        public bool HasError {
            get {
                return _hasError;
            }
            set {
                if (_hasError != value) {
                    _hasError = value;
                    OnPropertyChanged("HasError");
                }
            }
        }

        private bool _showProgress;
        public bool ShowProgress {
            get { return _showProgress; }
            set {
                if (_showProgress != value) {
                    _showProgress = value;
                    OnPropertyChanged("ShowProgress");
                }
            }
        }

        private int _percentComplete;
        public int PercentComplete {
            get { return _percentComplete; }
            set {
                if (_percentComplete != value) {
                    _percentComplete = value;
                    OnPropertyChanged("PercentComplete");
                }
            }
        }

        private bool _canPublish = true;

        public bool CanPublish {
            get { return _canPublish; }
            set {
                if (_canPublish != value) {
                    _canPublish = value;
                    OnPropertyChanged("CanPublish");
                }
            }
        }

        private GalleryServer _uploadHelper;

        public GalleryServer GalleryServer {
            get {
                if (_uploadHelper == null) {
                    _uploadHelper = new GalleryServer(
                        HttpUtility.CreateUserAgentString(Constants.UserAgentClient), 
                        _publishUrl, 
                        _proxyService);
                }
                return _uploadHelper;
            }
        }

        private string _status;

        public string Status {
            get { return _status; }
            set {
                if (_status != value) {
                    _status = value;
                    OnPropertyChanged("Status");
                }
            }
        }

        public void PushPackage() {
            PercentComplete = 0;
            ShowProgress = true;
            Status = "Uploading package...";
            HasError = false;
            CanPublish = false;

            Stream fileStream = _packageStream.Value;
            fileStream.Seek(0, SeekOrigin.Begin);

            try {
                GalleryServer.CreatePackage(PublishKey, fileStream, this, PushOnly == true ? (IPackageMetadata)null : _package);
            }
            catch (WebException e) {
                if (WebExceptionStatus.Timeout == e.Status) {
                    OnError(e);
                }
            }
        }

        public void OnCompleted() {
            ShowProgress = false;
            HasError = false;
            Status = (PushOnly == true) ? "Package pushed successfully." : "Package published successfully.";
        }

        public void OnError(Exception error) {
            PercentComplete = 100;
            ShowProgress = false;
            HasError = true;
            Status = error.Message;
            CanPublish = true;
        }

        public void OnNext(int value) {
            PercentComplete = value;
        }
    }
}