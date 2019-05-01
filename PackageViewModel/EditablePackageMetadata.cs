using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Input;
using NuGet.Packaging;
using NuGet.Packaging.Core;
using NuGet.Versioning;
using NuGetPackageExplorer.Types;
using NuGetPe;
using PackageType = NuGet.Packaging.Core.PackageType;

namespace PackageExplorerViewModel
{
    public sealed class EditablePackageMetadata : IPackageMetadata, IDataErrorInfo, INotifyPropertyChanged
    {
        private readonly Dictionary<string, string?> _propertyErrors = new Dictionary<string, string?>();
        private readonly IUIServices _uiServices;
        private string? _authors;
        private string? _copyright;
        private string? _description;
        private Uri? _iconUrl;
        private string _id;
        private string? _language;
        private Uri? _licenseUrl;
        private string? _owners;
        private Uri? _projectUrl;
        private string? _releaseNotes;
        private bool _requireLicenseAcceptance;
        private string? _summary;
        private string? _tags;
        private bool _serviceable;
        private string? _title;
        private NuGetVersion _version;
        private bool _isSigned;
        private ICollection<PackageDependencyGroup> _dependencySets;
        private ICollection<PackageReferenceSet> _packageAssemblyReferences;
        private ICollection<FrameworkReferenceGroup> _frameworkReferenceGroups;
        private Version? _minClientVersion;

        private readonly RelayCommand _showValidationResultsCommand;

        public ICommand ShowValidationResultsCommand => _showValidationResultsCommand;

        public EditablePackageMetadata(IPackageMetadata source, IUIServices uiServices)
        {
            _uiServices = uiServices;
            _showValidationResultsCommand = new RelayCommand(OnShowValidationResult, () => ValidationResult != null);

            _id = source.Id;
            _version = source.Version;
            PackageTypes = new ObservableCollection<PackageType>(source.PackageTypes);
            Title = source.Title;
            Authors = ConvertToString(source.Authors);
            Owners = ConvertToString(source.Owners);
            IconUrl = FixIconUrl(source.IconUrl);

            ProjectUrl = source.ProjectUrl;
            RequireLicenseAcceptance = source.RequireLicenseAcceptance;
            DevelopmentDependency = source.DevelopmentDependency;
            Description = source.Description;
            Summary = source.Summary;
            ReleaseNotes = source.ReleaseNotes;
            Copyright = source.Copyright;
            Language = source.Language;
            Tags = source.Tags;
            Serviceable = source.Serviceable;
            _dependencySets = new ObservableCollection<PackageDependencyGroup>(source.DependencyGroups);
            FrameworkAssemblies = new ObservableCollection<FrameworkAssemblyReference>(source.FrameworkReferences);
            _packageAssemblyReferences = new ObservableCollection<PackageReferenceSet>();
            ContentFiles = new ObservableCollection<ManifestContentFiles>(source.ContentFiles);
            _frameworkReferenceGroups = new ObservableCollection<FrameworkReferenceGroup>(source.FrameworkReferenceGroups);

            Repository = source.Repository;
            LicenseMetadata = source.LicenseMetadata;
            LicenseUrl = LicenseMetadata != null ? null : source.LicenseUrl; // This will be set for back compat, but should show up as null here

            if (source.PackageAssemblyReferences != null)
            {
                PackageAssemblyReferences.AddRange(source.PackageAssemblyReferences);
            }
            MinClientVersion = source.MinClientVersion;
        }

        public EditablePackageMetadata(IPackage source, IUIServices uiServices)
            : this((IPackageMetadata)source, uiServices)
        {
            // Zip Packages may be signed, we need to load that data async
            if (source is ISignaturePackage zip)
            {
                LoadSignatureData(zip);
            }
        }

        public async void LoadSignatureData(ISignaturePackage package)
        {
            if (package.IsSigned)
            {
                PublisherSignature = package.PublisherSignature;
                RepositorySignature = package.RepositorySignature;
                IsSigned = true;

                await Task.Run(() => package.VerifySignatureAsync());
                ValidationResult = new ValidationResultViewModel(package.VerificationResult);
            }
        }

        private void OnShowValidationResult()
        {
            Debug.Assert(ValidationResult != null, nameof(ValidationResult) + " != null");
            _uiServices.OpenSignatureValidationDialog(ValidationResult);
        }

        public string? Authors
        {
            get { return _authors; }
            set
            {
                if (string.IsNullOrWhiteSpace(value) && !PackageTypes.Any(pt => string.Equals(pt.Name, "SymbolsPackage", StringComparison.OrdinalIgnoreCase)))
                {
                    const string message = "Authors is required.";
                    SetError("Authors", message);
                    throw new ArgumentException(message);
                }

                SetError("Authors", null);
                if (_authors != value)
                {
                    _authors = value;
                    RaisePropertyChange("Authors");
                }
            }
        }

        public SignatureInfo? PublisherSignature
        {
            get { return _publisherCertificate; }
            set
            {
                if (_publisherCertificate != value)
                {
                    _publisherCertificate = value;
                    RaisePropertyChange(nameof(PublisherSignature));
                }
            }
        }

        public bool IsSigned
        {
            get => _isSigned;
            set
            {
                if (_isSigned != value)
                {
                    _isSigned = value;
                    RaisePropertyChange(nameof(IsSigned));
                }
            }
        }

        public ValidationResultViewModel? ValidationResult
        {
            get { return _validationResult; }
            set
            {
                if (_validationResult != value)
                {
                    _validationResult = value;
                    RaisePropertyChange(nameof(ValidationResult));
                }
            }
        }

        public SignatureInfo? RepositorySignature
        {
            get { return _repositoryCertificate; }
            set
            {
                if (_repositoryCertificate != value)
                {
                    _repositoryCertificate = value;
                    RaisePropertyChange(nameof(RepositorySignature));
                }
            }
        }

        public string? Owners
        {
            get { return _owners; }
            set
            {
                if (_owners != value)
                {
                    _owners = value;
                    RaisePropertyChange("Owners");
                }
            }
        }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Usage", "CA2227:CollectionPropertiesShouldBeReadOnly")]
        public ICollection<PackageReferenceSet> PackageAssemblyReferences
        {
            get
            {
                return _packageAssemblyReferences;
            }
            set
            {
                if (_packageAssemblyReferences != value)
                {
                    _packageAssemblyReferences = value;
                    RaisePropertyChange("PackageAssemblyReferences");
                }
            }
        }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Usage", "CA2227:CollectionPropertiesShouldBeReadOnly")]
        public ICollection<PackageDependencyGroup> DependencySets
        {
            get
            {
                return _dependencySets;
            }
            set
            {
                if (_dependencySets != value)
                {
                    _dependencySets = value;
                    RaisePropertyChange("DependencySets");
                }
            }
        }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Usage", "CA2227:CollectionPropertiesShouldBeReadOnly")]
        public ICollection<FrameworkReferenceGroup> FrameworkReferenceGroups
        {
            get
            {
                return _frameworkReferenceGroups;
            }
            set
            {
                if (_frameworkReferenceGroups != value)
                {
                    _frameworkReferenceGroups = value;
                    RaisePropertyChange(nameof(FrameworkReferenceGroups));
                }
            }
        }

        public ObservableCollection<FrameworkAssemblyReference> FrameworkAssemblies { get; private set; }

        #region IDataErrorInfo Members

        public string? Error
        {
            get { return null; }
        }

        public string? this[string columnName]
        {
            get { return IsValid(columnName); }
        }

        #endregion

        #region INotifyPropertyChanged Members

        public event PropertyChangedEventHandler PropertyChanged;
        private bool _developmentDependency;
        private RepositoryMetadata? _repository;
        private LicenseMetadata? _licenseMetadata;
        private SignatureInfo? _publisherCertificate;
        private ValidationResultViewModel? _validationResult;
        private SignatureInfo? _repositoryCertificate;

        #endregion

        #region IPackageMetadata Members

        public string Id
        {
            get { return _id; }
            set
            {
                try
                {
                    if (string.IsNullOrWhiteSpace(value))
                    {
                        throw new ArgumentException("Id is required.");
                    }
                }
                catch (Exception ex)
                {
                    SetError("Id", ex.Message);
                    throw;
                }

                SetError("Id", null);
                if (_id != value)
                {
                    _id = value;
                    RaisePropertyChange("Id");
                }
            }
        }

        public NuGetVersion Version
        {
            get { return _version; }
            set
            {
                if (value == null)
                {
                    const string message = "Version is required.";
                    SetError("Version", message);
                    throw new ArgumentException(message);
                }

                SetError("Version", null);
                if (_version != value)
                {
                    _version = value;
                    RaisePropertyChange("Version");
                }
            }
        }

        public string? Title
        {
            get { return _title; }
            set
            {
                if (_title != value)
                {
                    _title = value;
                    RaisePropertyChange("Title");
                }
            }
        }

        public Uri? IconUrl
        {
            get { return _iconUrl; }
            set
            {
                if (_iconUrl != value)
                {
                    _iconUrl = value;
                    RaisePropertyChange("IconUrl");
                }
            }
        }

        public Uri? LicenseUrl
        {
            get { return _licenseUrl; }
            set
            {
                if (_licenseUrl != value)
                {
                    _licenseUrl = value;
                    RaisePropertyChange("LicenseUrl");
                }
            }
        }

        public Uri? ProjectUrl
        {
            get { return _projectUrl; }
            set
            {
                if (_projectUrl != value)
                {
                    _projectUrl = value;
                    RaisePropertyChange("ProjectUrl");
                }
            }
        }

        public bool RequireLicenseAcceptance
        {
            get { return _requireLicenseAcceptance; }
            set
            {
                if (value != _requireLicenseAcceptance)
                {
                    _requireLicenseAcceptance = value;
                    RaisePropertyChange("RequireLicenseAcceptance");
                    RaisePropertyChange("LicenseUrl");
                }
            }
        }

        public bool DevelopmentDependency
        {
            get { return _developmentDependency; }
            set
            {
                if (value != _developmentDependency)
                {
                    _developmentDependency = value;
                    RaisePropertyChange("DevelopmentDependency");
                }
            }
        }

        public string? Description
        {
            get { return _description; }
            set
            {
                if (string.IsNullOrWhiteSpace(value))
                {
                    const string message = "Description is required.";
                    SetError("Description", message);
                    throw new ArgumentException(message);
                }

                SetError("Description", null);

                if (_description != value)
                {
                    _description = value;
                    RaisePropertyChange("Description");
                }
            }
        }

        public string? Summary
        {
            get { return _summary; }
            set
            {
                if (_summary != value)
                {
                    _summary = value;
                    RaisePropertyChange("Summary");
                }
            }
        }

        public string? ReleaseNotes
        {
            get { return _releaseNotes; }
            set
            {
                if (_releaseNotes != value)
                {
                    _releaseNotes = value;
                    RaisePropertyChange("ReleaseNotes");
                }
            }
        }

        public string? Copyright
        {
            get { return _copyright; }
            set
            {
                if (_copyright != value)
                {
                    _copyright = value;
                    RaisePropertyChange("Copyright");
                }
            }
        }

        public string? Language
        {
            get { return _language; }
            set
            {
                if (_language != value)
                {
                    _language = value;
                    RaisePropertyChange("Language");
                }
            }
        }

        public string? Tags
        {
            get { return _tags; }
            set
            {
                if (_tags != value)
                {
                    _tags = value;
                    RaisePropertyChange("Tags");
                }
            }
        }

        public bool Serviceable
        {
            get { return _serviceable; }
            set
            {
                if (value != _serviceable)
                {
                    _serviceable = value;
                    RaisePropertyChange("Serviceable");
                }
            }
        }

        public Version? MinClientVersion
        {
            get { return _minClientVersion; }
            set
            {
                if (_minClientVersion != value)
                {
                    _minClientVersion = value;
                    RaisePropertyChange("MinClientVersion");
                }
            }
        }

        IEnumerable<string> IPackageMetadata.Authors
        {
            get { return SplitString(Authors); }
        }

        IEnumerable<string> IPackageMetadata.Owners
        {
            get { return SplitString(Owners); }
        }

        IEnumerable<PackageDependencyGroup> IPackageMetadata.DependencyGroups
        {
            get { return DependencySets; }
        }

        IEnumerable<FrameworkAssemblyReference> IPackageMetadata.FrameworkReferences
        {
            get { return FrameworkAssemblies; }
        }

        IEnumerable<PackageReferenceSet> IPackageMetadata.PackageAssemblyReferences
        {
            get { return PackageAssemblyReferences; }
        }

        IEnumerable<FrameworkReferenceGroup> IPackageMetadata.FrameworkReferenceGroups
        {
            get { return FrameworkReferenceGroups; }
        }

        IEnumerable<ManifestContentFiles> IPackageMetadata.ContentFiles => ContentFiles;
        public ICollection<ManifestContentFiles> ContentFiles { get; set; }


        IEnumerable<PackageType> IPackageMetadata.PackageTypes => PackageTypes;
        public ICollection<PackageType> PackageTypes { get; set; }

        public RepositoryMetadata? Repository
        {
            get { return _repository; }
            set
            {
                _repository = value;
                RaisePropertyChange(nameof(Repository));
            }
        }

        public LicenseMetadata? LicenseMetadata
        {
            get { return _licenseMetadata; }
            set
            {
                _licenseMetadata = value;
                RaisePropertyChange(nameof(LicenseMetadata));
            }
        }

        #endregion



        private static Uri? FixIconUrl(Uri uri)
        {
            if (uri == null || uri.IsAbsoluteUri)
            {
                return uri;
            }

            var path = uri.OriginalString;
            if (path.StartsWith("//", StringComparison.Ordinal))
            {
                path = path.Substring(1);
            }

            var builder = new UriBuilder
            {
                Scheme = "https",
                Host = "www.nuget.org",
                Path = path
            };

            return builder.Uri;
        }

        private static IEnumerable<string> SplitString(string? text)
        {
            return text == null ? Enumerable.Empty<string>() : text.Split(',').Select(a => a.Trim());
        }

        private static string ConvertToString(IEnumerable<string> items)
        {
            return string.Join(", ", items);
        }

        public override string ToString()
        {
            return Id + " " + ManifestUtility.ReplaceMetadataWithToken(Version.ToFullString());
        }

        /// <summary>
        /// file name for V2 local feeds {id}.{version}
        /// </summary>
        public string FileName => Id + "." + ManifestUtility.ReplaceMetadataWithToken(Version.ToFullString());

        private string? IsValid(string propertyName)
        {
            if (propertyName == nameof(LicenseUrl) || propertyName == nameof(LicenseMetadata))
            {
                if (RequireLicenseAcceptance && LicenseUrl == null && LicenseMetadata == null)
                {
                    return "Enabling license acceptance requires a license url, expression, or file.";
                }
            }

            _propertyErrors.TryGetValue(propertyName, out var error);
            return error;
        }

        private void SetError(string property, string? error)
        {
            if (string.IsNullOrEmpty(error))
            {
                _propertyErrors.Remove(property);
            }
            else
            {
                _propertyErrors[property] = error;
            }
        }

        public void ResetErrors()
        {
            _propertyErrors.Clear();
        }

        private void RaisePropertyChange(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        public void ClearSignatures()
        {
            PublisherSignature = null;
            RepositorySignature = null;
            ValidationResult = null;
            IsSigned = false;
        }
    }
}
