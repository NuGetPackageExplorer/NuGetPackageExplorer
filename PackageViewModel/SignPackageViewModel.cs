using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Security.Cryptography.X509Certificates;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Input;
using NuGet.Common;
using NuGet.Packaging.Signing;
using NuGetPackageExplorer.Types;
using NuGetPe;

namespace PackageExplorerViewModel
{
    public class SignPackageViewModel : ViewModelBase, IDisposable
    {
        // https://github.com/NuGet/NuGet.Client/blob/a05632928e11d51b81d2299ba071334a16ce17a9/src/NuGet.Core/NuGet.Commands/SignCommand/CertificateProvider.cs#L22
        private const int ERROR_INVALID_PASSWORD_HRESULT = unchecked((int)0x80070056);

        private readonly PackageViewModel _packageViewModel;
        private readonly IUIServices _uiServices;
        private readonly ISettingsManager _settingsManager;
        private string? _certificateFileName;
        private X509Certificate2? _certificate;
        private string? _password;
        private bool _showPassword;
        private HashAlgorithmName _hashAlgorithmName = HashAlgorithmName.SHA256;
        private string? _timestampServer;
        private string? _status;
        private bool _hasError;
        private bool _showProgress;
        private bool _canSign;
        private CancellationTokenSource? _cts;
        private Timer? _certificateValidationTimer;
        private SemaphoreSlim? _certificateValidationSemaphore;

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
                        using var store = new X509Store(StoreName.My, StoreLocation.CurrentUser);
                        store.Open(OpenFlags.ReadOnly | OpenFlags.OpenExistingOnly);

                        var certificates = store.Certificates.Find(X509FindType.FindByThumbprint, settingsManager.SigningCertificate, validOnly: true);

                        if (certificates.Count > 0)
                        {
                            Certificate = certificates[0];
                            CertificateFileName = null;
                        }
                    }
                    catch { }
                }
            }

            TimestampServer = settingsManager.TimestampServer;

            if (Enum.TryParse(settingsManager.SigningHashAlgorithmName, out HashAlgorithmName hashAlgorithmName))
            {
                _hashAlgorithmName = hashAlgorithmName;
            }
        }

        public string Id => _packageViewModel.PackageMetadata.Id;

        public string Version => _packageViewModel.PackageMetadata.Version.ToFullString();

        public string? CertificateFileName
        {
            get => _certificateFileName;
            set
            {
                _certificateFileName = value;
                OnPropertyChanged();
                ValidateCertificate();
            }
        }

        public X509Certificate2? Certificate
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

        public string? Password
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

        public HashAlgorithmName HashAlgorithmName
        {
            get => _hashAlgorithmName;
            set
            {
                _hashAlgorithmName = value;
                OnPropertyChanged();
                ValidateCertificate();
            }
        }

        public IEnumerable<HashAlgorithmName> ValidHashAlgorithmNames
        {
            get
            {
                yield return HashAlgorithmName.SHA256;
                yield return HashAlgorithmName.SHA384;
                yield return HashAlgorithmName.SHA512;
            }
        }

        public string? TimestampServer
        {
            get => _timestampServer;
            set
            {
                _timestampServer = value;
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

        public string? Status
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
            DiagnosticsClient.TrackEvent("SignPackageViewModel_SelectCertificateFileCommandExecute");

            if (_uiServices.OpenFileDialog(Resources.SelectCertificate, "Certificate (*.pfx, *.p12)|*.pfx;*.p12|All files (*.*)|*.*", out var fileName))
            {
                CertificateFileName = fileName;
            }
        }

        private void SelectCertificateStoreCommandExecute()
        {
            DiagnosticsClient.TrackEvent("SignPackageViewModel_SelectCertificateFileCommandExecute");

            try
            {
                using var store = new X509Store(StoreName.My, StoreLocation.CurrentUser);
                store.Open(OpenFlags.ReadOnly | OpenFlags.OpenExistingOnly);

                // https://github.com/NuGet/NuGet.Client/blob/adfe6d5c37834e8eb11453518e4508a534c15f8d/src/NuGet.Core/NuGet.Commands/SignCommand/SignCommandRunner.cs#L271-L283
                var collection = new X509Certificate2Collection();

                foreach (var certificate in store.Certificates)
                {
                    if (IsCertificateValidForNuGet(certificate))
                    {
                        collection.Add(certificate);
                    }
                }

                var certificates = X509Certificate2UI.SelectFromCollection(
                    collection,
                    Resources.ChooseCertificate_Title,
                    Resources.ChooseCertificate_Description,
                    X509SelectionFlag.SingleSelection);

                if (certificates.Count > 0)
                {
                    Certificate = certificates[0];
                    CertificateFileName = null;
                    ShowPassword = false;
                }
            }
            catch (Exception ex)
            {
                OnError(ex);
            }
        }

        private static bool IsCertificateValidForNuGet(X509Certificate2 certificate) =>
            CertificateUtility.IsValidForPurposeFast(certificate, Oids.CodeSigningEku) &&
            CertificateUtility.IsCertificatePublicKeyValid(certificate) &&
            CertificateUtility.IsSignatureAlgorithmSupported(certificate) &&
            !CertificateUtility.HasExtendedKeyUsage(certificate, Oids.LifetimeSigningEku) &&
            !CertificateUtility.IsCertificateValidityPeriodInTheFuture(certificate);

        private void ShowCertificateCommandExecute()
        {
            DiagnosticsClient.TrackEvent("SignPackageViewModel_ShowCertificateCommandExecute");

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
        public async Task<string?> SignPackage()
        {
            ShowProgress = true;
            CanSign = false;
            HasError = false;
            Status = _packageViewModel.IsSigned ? Resources.SigningPackageAndRemoveSignature : Resources.SigningPackage;

            var cts = _cts = new CancellationTokenSource();
            var token = cts.Token;

            try
            {
                using var tempCertificate = new X509Certificate2(Certificate);
                using var signRequest = new AuthorSignPackageRequest(tempCertificate, HashAlgorithmName);
                var packagePath = _packageViewModel.GetCurrentPackageTempFile();
                var originalPackageCopyPath = Path.GetTempFileName();

                ITimestampProvider? timestampProvider = null;
                if (!string.IsNullOrEmpty(TimestampServer))
                {
                    timestampProvider = new Rfc3161TimestampProvider(new Uri(TimestampServer));
                }
                var signatureProvider = new X509SignatureProvider(timestampProvider);

                using (var options = SigningOptions.CreateFromFilePaths(
                    inputPackageFilePath: packagePath,
                    outputPackageFilePath: originalPackageCopyPath,
                    overwrite: true,
                    signatureProvider: signatureProvider,
                    logger: NullLogger.Instance))
                {
                    await SigningUtility.SignAsync(options, signRequest, token);
                }

                File.Delete(packagePath);

                token.ThrowIfCancellationRequested();

                return originalPackageCopyPath;
            }
            catch (Exception e)
            {
                OnError(e);
                return null;
            }
            finally
            {
                cts.Dispose();
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
            Debug.Assert(_certificateValidationSemaphore != null, nameof(_certificateValidationSemaphore) + " != null");

            _certificateValidationSemaphore.Wait();

            try
            {
                Clear();

                X509Certificate2? certificate;

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
                using (var signRequest = new AuthorSignPackageRequest(tempCertificate, HashAlgorithmName))
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
                    OnError(new Exception(Resources.PasswordRequired));
                    ShowPassword = true;
                }
                else
                {
                    OnError(new Exception(Resources.PasswordIncorrect));
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
            _settingsManager.TimestampServer = TimestampServer;
            _settingsManager.SigningHashAlgorithmName = HashAlgorithmName.ToString();

            try
            {
                _cts?.Cancel();
            }
            catch { }

            _certificateValidationTimer?.Dispose();
            _certificateValidationSemaphore?.Dispose();
            _certificate?.Dispose();
        }
    }
}
