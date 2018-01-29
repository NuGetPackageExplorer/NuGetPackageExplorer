using System;
using System.Collections.ObjectModel;
using System.Diagnostics.CodeAnalysis;
using System.Net;
using System.Threading.Tasks;
using NuGetPackageExplorer.Types;
using NuGet.Packaging;
using NuGet.Protocol.Core.Types;
using NuGet.Common;
using PackageExplorerViewModel.Types;

namespace PackageExplorerViewModel
{
    public sealed class PublishPackageViewModel : ViewModelBase, IObserver<int>, IDisposable
    {
        private readonly MruPackageSourceManager _mruSourceManager;
        private readonly IPackageMetadata _package;
        private readonly string _packageFilePath;
        private readonly ISettingsManager _settingsManager;
        private readonly ICredentialManager _credentialManager;
        private bool _canPublish = true;
        private bool _hasError;
        private string _publishKey;
        private string _publishCredentialUsername;
        private string _publishCredentialPassword;
        private bool _isAuthSet;
        private bool? _publishAsUnlisted = true;
        private bool? _useApiKey = true;
        private string _selectedPublishItem;
        private bool _showProgress;
        private string _status;
        private bool _suppressReadingApiKey;

        public PublishPackageViewModel(
            MruPackageSourceManager mruSourceManager,
            ISettingsManager settingsManager,
            ICredentialManager credentialManager,
            PackageViewModel viewModel)
        {
            _mruSourceManager = mruSourceManager;
            _settingsManager = settingsManager;
            _credentialManager = credentialManager;
            _package = viewModel.PackageMetadata;
            _packageFilePath = viewModel.GetCurrentPackageTempFile();
            SelectedPublishItem = _mruSourceManager.ActivePackageSource;
            PublishAsUnlisted = _settingsManager.PublishAsUnlisted;
            UseApiKey = _settingsManager.UseApiKey;
        }

        public string PublishKey
        {
            get { return _publishKey; }
            set
            {
                if (_publishKey != value)
                {
                    _publishKey = value;
                    OnPropertyChanged(nameof(PublishKey));
                    CheckIfAuthIsSet();
                }
            }
        }
        public string PublishCredentialUsername
        {
            get { return _publishCredentialUsername; }
            set
            {
                if (_publishCredentialUsername != value)
                {
                    _publishCredentialUsername = value;
                    OnPropertyChanged(nameof(PublishCredentialUsername));
                    CheckIfAuthIsSet();
                }
            }
        }
        public string PublishCredentialPassword
        {
            get { return _publishCredentialPassword; }
            set
            {
                if (_publishCredentialPassword != value)
                {
                    _publishCredentialPassword = value;
                    OnPropertyChanged(nameof(PublishCredentialPassword));
                    CheckIfAuthIsSet();
                }
            }
        }

        [SuppressMessage("Microsoft.Design", "CA1056:UriPropertiesShouldNotBeStrings")]
        public string PublishUrl
        {
            get { return _mruSourceManager.ActivePackageSource; }
            set
            {
                if (_mruSourceManager.ActivePackageSource != value)
                {
                    _mruSourceManager.ActivePackageSource = value;
                    OnPropertyChanged(nameof(PublishUrl));
                }
            }
        }

        public string SelectedPublishItem
        {
            get { return _selectedPublishItem; }
            set
            {
                if (_selectedPublishItem != value)
                {
                    _selectedPublishItem = value;
                    OnPropertyChanged(nameof(SelectedPublishItem));

                    if (value != null)
                    {
                        // store the selected source into settings
                        PublishUrl = value;

                        if (!_suppressReadingApiKey)
                        {
                            // when the selection change, we retrieve the API key for that source
                            var key = _settingsManager.ReadApiKey(value);
                            if (!string.IsNullOrEmpty(key))
                            {
                                PublishKey = key;
                            }
                        }
                    }
                }
            }
        }

        public ObservableCollection<string> PublishSources
        {
            get { return _mruSourceManager.PackageSources; }
        }

        public bool? PublishAsUnlisted
        {
            get { return _publishAsUnlisted; }
            set
            {
                if (_publishAsUnlisted != value)
                {
                    _publishAsUnlisted = value;
                    OnPropertyChanged(nameof(PublishAsUnlisted));
                }
            }
        }
        public bool? UseApiKey
        {
            get { return _useApiKey; }
            set
            {
                if (_useApiKey != value)
                {
                    _useApiKey = value;
                    OnPropertyChanged(nameof(UseApiKey));
                    CheckIfAuthIsSet();
                }
            }
        }
        public bool? UseCredentials

        {
            get { return !UseApiKey; }
            set
            {
                UseApiKey = !value;
            }
        }

        public string Id
        {
            get { return _package.Id; }
        }

        public string Version
        {
            get { return _package.Version.ToFullString(); }
        }

        public bool HasError
        {
            get { return _hasError; }
            set
            {
                if (_hasError != value)
                {
                    _hasError = value;
                    OnPropertyChanged(nameof(HasError));
                }
            }
        }

        public bool ShowProgress
        {
            get { return _showProgress; }
            set
            {
                if (_showProgress != value)
                {
                    _showProgress = value;
                    OnPropertyChanged(nameof(ShowProgress));
                }
            }
        }

        public bool CanPublish
        {
            get { return _canPublish; }
            set
            {
                if (_canPublish != value)
                {
                    _canPublish = value;
                    OnPropertyChanged(nameof(CanPublish));
                }
            }
        }
        public bool IsAuthSet
        {
            get { return _isAuthSet; }
            set
            {
                if (_isAuthSet != value)
                {
                    _isAuthSet = value;
                    OnPropertyChanged(nameof(IsAuthSet));
                }
            }
        }

        private void CheckIfAuthIsSet()
        {
            IsAuthSet = (UseApiKey.HasValue && UseApiKey.Value && !string.IsNullOrWhiteSpace(PublishKey)) || (UseCredentials.HasValue && UseCredentials.Value && !string.IsNullOrWhiteSpace(PublishCredentialPassword)); ;
        }

        public string Status
        {
            get { return _status; }
            set
            {
                if (_status != value)
                {
                    _status = value;
                    OnPropertyChanged(nameof(Status));
                }
            }
        }

        #region IObserver<int> Members

        public void OnCompleted()
        {
            ShowProgress = false;
            HasError = false;
            Status = (PublishAsUnlisted == true) ? "Package published and unlisted successfully." : "Package published successfully.";
            if (UseApiKey.HasValue && UseApiKey.Value)
            {
                _settingsManager.WriteApiKey(PublishUrl, PublishKey);
            }
            CanPublish = true;
        }

        public void OnError(Exception error)
        {
            ShowProgress = false;
            HasError = true;
            Status = error.Message;
            CanPublish = true;
        }

        public void OnNext(int value)
        {
        }

        #endregion

        public async Task PushPackage()
        {
            ShowProgress = true;
            Status = (PublishAsUnlisted == true) ? "Publishing and unlisting package..." : "Publishing package...";
            HasError = false;
            CanPublish = false;

            try
            {
                var url = PublishUrl;

                if (UseCredentials.HasValue && UseCredentials.Value)
                {
                    _credentialManager.Add(new NetworkCredential(PublishCredentialUsername, PublishCredentialPassword), new Uri(url));
                }

                var repository = PackageRepositoryFactory.CreateRepository(url, _credentialManager);
                var updateResource = await repository.GetResourceAsync<PackageUpdateResource>();

                await updateResource.Push(_packageFilePath, null, 999, false, s => PublishKey, s => PublishKey, NullLogger.Instance);

                if (PublishAsUnlisted == true)
                {
                    await updateResource.Delete(Id, Version, s => PublishKey, s => true, NullLogger.Instance);
                }

                OnCompleted();
            }
            catch (Exception exception)
            {
                OnError(exception);
            }
            finally
            {
                // add the publish url to the list
                _mruSourceManager.NotifyPackageSourceAdded(PublishUrl);

                // this is to make sure the combo box doesn't goes blank after publishing
                try
                {
                    _suppressReadingApiKey = true;
                    SelectedPublishItem = PublishUrl;
                }
                finally
                {
                    _suppressReadingApiKey = false;
                }
            }
        }

        public void Dispose()
        {
            _settingsManager.PublishAsUnlisted = (bool)PublishAsUnlisted;
            _settingsManager.UseApiKey = UseApiKey ?? true;
        }
    }
}
