using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using NuGet;

namespace PackageExplorerViewModel
{
    public sealed class EditablePackageMetadata : IPackageMetadata, IDataErrorInfo, INotifyPropertyChanged
    {
        private readonly Dictionary<string, string> _propertyErrors = new Dictionary<string, string>();
        private string _authors;
        private string _copyright;
        private string _description;
        private Uri _iconUrl;
        private string _id;
        private string _language;
        private Uri _licenseUrl;
        private string _owners;
        private Uri _projectUrl;
        private string _releaseNotes;
        private bool _requireLicenseAcceptance;
        private string _summary;
        private string _tags;
        private bool _serviceable;
        private string _title;
        private SemanticVersion _version;
        private ICollection<PackageDependencySet> _dependencySets;
        private ICollection<PackageReferenceSet> _packageAssemblyReferences;
        private Version _minClientVersion;

        public EditablePackageMetadata()
        {
        }

        public EditablePackageMetadata(IPackageMetadata source)
        {
            CopyFrom(source);
        }

        public string Authors
        {
            get { return _authors; }
            set
            {
                if (String.IsNullOrWhiteSpace(value))
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

        public string Owners
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
        public ICollection<PackageDependencySet> DependencySets 
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

        public ObservableCollection<FrameworkAssemblyReference> FrameworkAssemblies { get; private set; }

        #region IDataErrorInfo Members

        public string Error
        {
            get { return null; }
        }

        public string this[string columnName]
        {
            get { return IsValid(columnName); }
        }

        #endregion

        #region INotifyPropertyChanged Members

        public event PropertyChangedEventHandler PropertyChanged;
        private bool _developmentDependency;

        #endregion

        #region IPackageMetadata Members

        public string Id
        {
            get { return _id; }
            set
            {
                try
                {
                    if (String.IsNullOrWhiteSpace(value))
                    {
                        throw new ArgumentException("Id is required.");
                    }

                    PackageIdValidator.ValidatePackageId(value);
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

        public SemanticVersion Version
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

        public string Title
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

        public Uri IconUrl
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

        public Uri LicenseUrl
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

        public Uri ProjectUrl
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

        public string Description
        {
            get { return _description; }
            set
            {
                if (String.IsNullOrWhiteSpace(value))
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

        public string Summary
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

        public string ReleaseNotes
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

        public string Copyright
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

        public string Language
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

        public string Tags
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

        public Version MinClientVersion
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

        IEnumerable<PackageDependencySet> IPackageMetadata.DependencySets
        {
            get { return DependencySets; }
        }

        IEnumerable<FrameworkAssemblyReference> IPackageMetadata.FrameworkAssemblies
        {
            get { return FrameworkAssemblies; }
        }

        IEnumerable<PackageReferenceSet> IPackageMetadata.PackageAssemblyReferences
        {
            get { return PackageAssemblyReferences; }
        }

        #endregion

        public void CopyFrom(IPackageMetadata source)
        {
            Id = source.Id;
            Version = source.Version;
            Title = source.Title;
            Authors = ConvertToString(source.Authors);
            Owners = ConvertToString(source.Owners);
            IconUrl = FixIconUrl(source.IconUrl);
            LicenseUrl = source.LicenseUrl;
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
            DependencySets = new ObservableCollection<PackageDependencySet>(source.DependencySets);
            FrameworkAssemblies = new ObservableCollection<FrameworkAssemblyReference>(source.FrameworkAssemblies);
            PackageAssemblyReferences = new ObservableCollection<PackageReferenceSet>();
            if (source.PackageAssemblyReferences != null)
            {
                PackageAssemblyReferences.AddRange(source.PackageAssemblyReferences);
            }
            MinClientVersion = source.MinClientVersion;
        }

        private static Uri FixIconUrl(Uri uri)
        {
            if (uri == null || uri.IsAbsoluteUri)
            {
                return uri;
            }

            string path = uri.OriginalString;
            if (path.StartsWith("//", StringComparison.Ordinal))
            {
                path = path.Substring(1);
            }

            var builder = new UriBuilder
                          {
                              Scheme = "http",
                              Host = "www.nuget.org",
                              Path = path
                          };

            return builder.Uri;
        }

        private static IEnumerable<string> SplitString(string text)
        {
            return text == null ? Enumerable.Empty<string>() : text.Split(',').Select(a => a.Trim());
        }

        private static string ConvertToString(IEnumerable<string> items)
        {
            return String.Join(", ", items);
        }

        public override string ToString()
        {
            return Id + "." + Version;
        }

        private string IsValid(string propertyName)
        {
            if (propertyName == "LicenseUrl")
            {
                if (RequireLicenseAcceptance && LicenseUrl == null)
                {
                    return "Enabling license acceptance requires a license url.";
                }
            }

            string error;
            _propertyErrors.TryGetValue(propertyName, out error);
            return error;
        }

        private void SetError(string property, string error)
        {
            if (String.IsNullOrEmpty(error))
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
            if (PropertyChanged != null)
            {
                PropertyChanged(this, new PropertyChangedEventArgs(propertyName));
            }
        }
    }
}