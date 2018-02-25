using NuGet.Common;
using NuGet.Packaging.Signing;
using NuGetPackageExplorer.Types;
using System;
using System.IO;
using System.Security.Cryptography.X509Certificates;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Input;

namespace PackageExplorerViewModel
{
    public class SignPackageViewModel : ViewModelBase, IDisposable
    {
        private readonly PackageViewModel _packageViewModel;
        private readonly IUIServices _uiServices;
        private string _certificateFileName;
        private X509Certificate2 _certificate;
        private string _password;
        private ICommand _selectCertificateCommand;
        private string _status;
        private bool _hasError;
        private bool _showProgress;
        private bool _canSign;
        private Timer _certificateValidationTimer;
        private SemaphoreSlim _certificateValidationSemaphore;

        public SignPackageViewModel(PackageViewModel viewModel, IUIServices uiServices)
        {
            _uiServices = uiServices;
            _packageViewModel = viewModel;
        }

        public string Id => _packageViewModel.PackageMetadata.Id;

        public string Version => _packageViewModel.PackageMetadata.Version.ToFullString();

        public string CertificateFileName
        {
            get => _certificateFileName;
            set
            {
                _certificateFileName = value;
                OnPropertyChanged();
                ValidateCertificate();
            }
        }

        public X509Certificate2 Certificate
        {
            get => _certificate;
            set
            {
                if (_certificate != null)
                {
                    _certificate.Dispose();
                }

                _certificate = value;
                OnPropertyChanged();
            }
        }

        public string Password
        {
            get => _password;
            set
            {
                _password = value;
                OnPropertyChanged();
                ValidateCertificate();
            }
        }

        public ICommand SelectCertificateCommand
        {
            get
            {
                if (_selectCertificateCommand == null)
                {
                    _selectCertificateCommand = new RelayCommand(SelectCertificateCommandExecute);
                }
                return _selectCertificateCommand;
            }
        }

        public bool HasError
        {
            get => _hasError;
            set
            {
                if (_hasError != value)
                {
                    _hasError = value;
                    OnPropertyChanged();
                }
            }
        }

        public string Status
        {
            get => _status;
            set
            {
                if (_status != value)
                {
                    _status = value;
                    OnPropertyChanged();
                }
            }
        }

        public bool ShowProgress
        {
            get => _showProgress;
            set
            {
                _showProgress = value;
                OnPropertyChanged();
            }
        }

        public bool CanSign
        {
            get => _canSign;
            set
            {
                _canSign = value;
                OnPropertyChanged();
            }
        }

        private void SelectCertificateCommandExecute()
        {
            if (_uiServices.OpenFileDialog("Select Certificate", "Certificate (*.pfx, *.p12)|*.pfx;*.p12|All files (*.*)|*.*", out var fileName))
            {
                CertificateFileName = fileName;
            }
        }

        // https://github.com/NuGet/NuGet.Client/blob/4c0c9658445573845ddbeff5656e4b3129f727a2/src/NuGet.Core/NuGet.Commands/SignCommand/SignCommandRunner.cs
        public async Task<string> SignPackage()
        {
            ShowProgress = true;
            CanSign = false;
            Status = "Signing package...";

            try
            {
                // change to AuthorSignPackageRequest in NuGet.Client is updated
                using (var signRequest = new SignPackageRequest(Certificate, HashAlgorithmName.SHA256))
                {
                    SigningUtility.Verify(signRequest, NullLogger.Instance);

                    var packagePath = _packageViewModel.GetCurrentPackageTempFile();
                    var originalPackageCopyPath = Path.GetTempFileName();

                    File.Copy(packagePath, originalPackageCopyPath, overwrite: true);

                    using (var packageReadStream = File.OpenRead(packagePath))
                    using (var packageWriteStream = File.Open(originalPackageCopyPath, FileMode.Open))
                    using (var package = new SignedPackageArchive(packageReadStream, packageWriteStream))
                    {
                        var signer = new Signer(package, new X509SignatureProvider(null));
                        await Task.Run(() => signer.SignAsync(signRequest, NullLogger.Instance, CancellationToken.None));
                    }

                    return originalPackageCopyPath;
                }
            }
            catch (Exception e)
            {
                OnError(e);
                return null;
            }
            finally
            {
                ShowProgress = false;
                CanSign = true;
            }
        }

        private void ValidateCertificate()
        {
            if (_certificateValidationTimer == null)
            {
                _certificateValidationSemaphore = new SemaphoreSlim(1);
                _certificateValidationTimer = new Timer(ValidateCertificateCallback);
            }
            _certificateValidationTimer.Change(250, Timeout.Infinite);
        }

        private void ValidateCertificateCallback(object state)
        {
            _certificateValidationSemaphore.Wait();

            try
            {
                var certificateFileName = CertificateFileName;
                var password = Password;

                if (string.IsNullOrEmpty(certificateFileName) || password == null)
                {
                    return;
                }

                Clear();
                Certificate = null;

                try
                {
                    // this throws if password is wrong
                    var certificate = new X509Certificate2(certificateFileName, password);

                    // change to AuthorSignPackageRequest in NuGet.Client is updated
                    // this also disposes the certificate...
                    using (var signRequest = new SignPackageRequest(certificate, HashAlgorithmName.SHA256))
                    {
                        SigningUtility.Verify(signRequest, NullLogger.Instance);
                    }

                    Certificate = new X509Certificate2(certificateFileName, password);
                    CanSign = true;
                }
                catch (Exception ex)
                {
                    OnError(ex);
                }
            }
            finally
            {
                _certificateValidationSemaphore.Release();
            }
        }

        private void OnError(Exception ex)
        {
            ShowProgress = false;
            HasError = true;
            Status = ex.Message.Trim();
        }

        private void Clear()
        {
            ShowProgress = false;
            HasError = false;
            Status = null;
        }

        public void Dispose()
        {
            _certificateValidationTimer?.Dispose();
            _certificateValidationSemaphore?.Dispose();
        }
    }
}
