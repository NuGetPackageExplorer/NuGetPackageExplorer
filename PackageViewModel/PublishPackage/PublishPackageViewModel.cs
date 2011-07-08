using System;
using System.Collections.ObjectModel;
using System.IO;
using System.Net;
using NuGet;
using NuGetPackageExplorer.Types;

namespace PackageExplorerViewModel {
    public class PublishPackageViewModel : ViewModelBase, IObserver<int> {
        private readonly IPackageMetadata _package;
        private readonly Lazy<Stream> _packageStream;
        private readonly IProxyService _proxyService;
        private readonly MruPackageSourceManager _mruSourceManager;
        private readonly ISettingsManager _settingsManager;
        private string _publishKey;
        private string _selectedPublishItem;
        private bool _suppressReadingApiKey;

        public PublishPackageViewModel(
            MruPackageSourceManager mruSourceManager, 
            ISettingsManager settingsManager,
            PackageViewModel viewModel) {

            _mruSourceManager = mruSourceManager;
            _settingsManager = settingsManager;
            _package = viewModel.PackageMetadata;
            _packageStream = new Lazy<Stream>(viewModel.GetCurrentPackageStream);
            _proxyService = viewModel.ProxyService;
            SelectedPublishItem = _mruSourceManager.ActivePackageSource;
        }

        public string PublishKey {
            get { return _publishKey; }
            set {
                if (_publishKey != value) {
                    _publishKey = value;
                    OnPropertyChanged("PublishKey");
                }
            }
        }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Design", "CA1056:UriPropertiesShouldNotBeStrings")]
        public string PublishUrl {
            get { return _mruSourceManager.ActivePackageSource; }
            set {
                if (_mruSourceManager.ActivePackageSource != value) {
                    _mruSourceManager.ActivePackageSource = value;
                    OnPropertyChanged("PublishUrl");
                }
            }
        }

        public string SelectedPublishItem {
            get {
                return _selectedPublishItem;
            }
            set {
                if (_selectedPublishItem != value) {
                    _selectedPublishItem = value;
                    OnPropertyChanged("SelectedPublishItem");

                    if (value != null) {
                        // store the selected source into settings
                        PublishUrl = value;

                        if (!_suppressReadingApiKey) {
                            // when the selection change, we retrieve the API key for that source
                            string key = _settingsManager.ReadApiKey(value);
                            if (!String.IsNullOrEmpty(key)) {
                                PublishKey = key;
                            }
                        }
                    }
                }
            }
        }

        public ObservableCollection<string> PublishSources {
            get {
                return _mruSourceManager.PackageSources;
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
                if (_uploadHelper == null || !PublishUrl.Equals(_uploadHelper.OriginalSource, StringComparison.OrdinalIgnoreCase)) {
                    _uploadHelper = new GalleryServer(
                        HttpUtility.CreateUserAgentString(Constants.UserAgentClient), 
                        PublishUrl, 
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
                GalleryServer.CreatePackage(
                    PublishKey,
                    fileStream,
                    this,
                    PushOnly == true ? (IPackageMetadata)null : _package);
            }
            catch (WebException e) {
                if (e.Status == WebExceptionStatus.Timeout) {
                    OnError(e);
                }
            }
            finally {
                // add the publish url to the list
                _mruSourceManager.NotifyPackageSourceAdded(PublishUrl);

                // this is to make sure the combox box doesn't goes blank after publishing
                try {
                    _suppressReadingApiKey = true;
                    SelectedPublishItem = PublishUrl;
                }
                finally {
                    _suppressReadingApiKey = false;
                }
            }
        }

        public void OnCompleted() {
            ShowProgress = false;
            HasError = false;
            Status = (PushOnly == true) ? "Package pushed successfully." : "Package published successfully.";
            _settingsManager.WriteApiKey(PublishUrl, PublishKey);

            CanPublish = true;
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
