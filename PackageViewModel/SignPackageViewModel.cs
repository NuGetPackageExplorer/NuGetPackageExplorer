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
        // https://github.com/NuGet/NuGet.Client/blob/a05632928e11d51b81d2299ba071334a16ce17a9/src/NuGet.Core/NuGet.Commands/SignCommand/CertificateProvider.cs#L22
        private const int ERROR_INVALID_PASSWORD_HRESULT = unchecked((int)0x80070056);

        private readonly PackageViewModel _packageViewModel;
        private readonly IUIServices _uiServices;
        private readonly ISettingsManager _settingsManager;
        private string _certificateFileName;
        private X509Certificate2 _certificate;
        private string _password;
        private bool _showPassword;
        private string _status;
        private bool _hasError;
        private bool _showProgress;
        private bool _canSign;
        private Timer _certificateValidationTimer;
        private SemaphoreSlim _certificateValidationSemaphore;

        public SignPackageViewModel(PackageViewModel viewModel, IUIServices uiServices, ISettingsManager settingsManager)
        {
            _uiServices = uiServices;
            _settingsManager = settingsManager;
            _packageViewModel = viewModel;

            SelectCertificateFileCommand = new RelayCommand(SelectCertificateFileCommandExecute);
            SelectCertificateStoreCommand = new RelayCommand(SelectCertificateStoreCommandExecute);
            ShowCertificateCommand = new RelayCommand(ShowCertificateCommandExecute);

            if (!string.IsNullOrEmpty(settingsManager.SigningCertificate))
            {
                if (File.Exists(settingsManager.SigningCertificate))
                {
                    CertificateFileName = settingsManager.SigningCertificate;
                }
                else
                {
                    try
                    {
                        using (var store = new X509Store(StoreName.My, StoreLocation.CurrentUser))
                        {
                            store.Open(OpenFlags.ReadOnly | OpenFlags.OpenExistingOnly);

                            var certificates = store.Certificates.Find(X509FindType.FindByThumbprint, settingsManager.SigningCertificate, validOnly: true);

                            if (certificates.Count > 0)
                            {
                                Certificate = certificates[0];
                                CertificateFileName = null;
                            }
                        }
                    } catch { }
                }
            }
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
                if (_certificate != value)
                {
                    _certificate?.Dispose();
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

        public bool ShowPassword
        {
            get => _showPassword;
            set
            {
                _showPassword = value;
                OnPropertyChanged();
            }
        }

        public ICommand SelectCertificateFileCommand { get; }

        public ICommand SelectCertificateStoreCommand { get; }

        public ICommand ShowCertificateCommand { get; }

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

        private void SelectCertificateFileCommandExecute()
        {
            if (_uiServices.OpenFileDialog("Select Certificate", "Certificate (*.pfx, *.p12)|*.pfx;*.p12|All files (*.*)|*.*", out var fileName))
            {
                CertificateFileName = fileName;
            }
        }

        private void SelectCertificateStoreCommandExecute()
        {
            try
            {
                using (var store = new X509Store(StoreName.My, StoreLocation.CurrentUser))
                {
                    store.Open(OpenFlags.ReadOnly | OpenFlags.OpenExistingOnly);

                    // https://github.com/NuGet/NuGet.Client/blob/adfe6d5c37834e8eb11453518e4508a534c15f8d/src/NuGet.Core/NuGet.Commands/SignCommand/SignCommandRunner.cs#L271-L283
                    var collection = new X509Certificate2Collection();

                    foreach (var certificate in store.Certificates)
                    {
                        if (CertificateUtility.IsValidForPurposeFast(certificate, Oids.CodeSigningEku))
                        {
                            collection.Add(certificate);
                        }
                    }

                    var certificates = X509Certificate2UI.SelectFromCollection(
                        collection,
                        "Choose a Certificate for Package Signing",
                        "Provide the code signing certificate for signing the package.",
                        X509SelectionFlag.SingleSelection);

                    if (certificates.Count > 0)
                    {
                        Certificate = certificates[0];
                        CertificateFileName = null;
                        ShowPassword = false;
                    }
                }
            }
            catch (Exception ex)
            {
                OnError(ex);
            }
        }

        private void ShowCertificateCommandExecute()
        {
            var certificate = Certificate;
            if (certificate != null)
            {
                try
                {
                    X509Certificate2UI.DisplayCertificate(certificate);
                }
                catch { }
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
                // change to AuthorSignPackageRequest when NuGet.Client is updated
                using (var tempCertificate = new X509Certificate2(Certificate))
                using (var signRequest = new SignPackageRequest(tempCertificate, GetHashAlgorithmName(tempCertificate)))
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
                Clear();

                X509Certificate2 certificate;

                var certificateFileName = CertificateFileName;
                if (string.IsNullOrEmpty(certificateFileName))
                {
                    certificate = Certificate;
                }
                else
                {
                    var password = Password;
                    if (string.IsNullOrEmpty(password))
                    {
                        certificate = new X509Certificate2(certificateFileName);
                    }
                    else
                    {
                        // this throws if the password is wrong
                        certificate = new X509Certificate2(certificateFileName, password);
                    }
                }

                if (certificate == null)
                {
                    return;
                }

                using (var tempCertificate = new X509Certificate2(certificate))
                // change to AuthorSignPackageRequest when NuGet.Client is updated
                using (var signRequest = new SignPackageRequest(tempCertificate, GetHashAlgorithmName(tempCertificate)))
                {
                    SigningUtility.Verify(signRequest, NullLogger.Instance);
                }

                Certificate = certificate;
                CanSign = true;
            }
            catch (System.Security.Cryptography.CryptographicException ex) when (ex.HResult == ERROR_INVALID_PASSWORD_HRESULT)
            {
                Certificate = null;

                if (!ShowPassword)
                {
                    OnError(new Exception("Password required"));
                    ShowPassword = true;
                }
                else
                {
                    OnError(new Exception("Invalid Password"));
                }
            }
            catch (Exception ex)
            {
                Certificate = null;
                OnError(ex);
            }
            finally
            {
                _certificateValidationSemaphore.Release();
            }
        }

        // https://github.com/NuGet/NuGet.Client/blob/894388a598834a1fe8a0e483a2bc050c05693313/src/NuGet.Core/NuGet.Packaging/Signing/Utility/CertificateUtility.cs#L112-L124
        private static HashAlgorithmName GetHashAlgorithmName(X509Certificate2 certificate)
        {
            switch (certificate.SignatureAlgorithm.Value)
            {
                case Oids.Sha256WithRSAEncryption:
                    return HashAlgorithmName.SHA256;
                case Oids.Sha384WithRSAEncryption:
                    return HashAlgorithmName.SHA384;
                case Oids.Sha512WithRSAEncryption:
                    return HashAlgorithmName.SHA512;
                default:
                    return HashAlgorithmName.Unknown;
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
            if (Certificate != null)
            {
                if (!string.IsNullOrEmpty(CertificateFileName))
                {
                    _settingsManager.SigningCertificate = CertificateFileName;
                }
                else
                {
                    _settingsManager.SigningCertificate = Certificate.Thumbprint;
                }
            }

            _certificateValidationTimer?.Dispose();
            _certificateValidationSemaphore?.Dispose();
            _certificate?.Dispose();
        }
    }
}
