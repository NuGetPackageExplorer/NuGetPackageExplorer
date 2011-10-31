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
        private string _title;
        private SemanticVersion _version;

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
                    string message = "Authors is required.";
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

        public ObservableCollection<AssemblyReference> PackageAssemblyReferences { get; private set; }

        public ObservableCollection<PackageDependency> Dependencies { get; private set; }

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
                    string message = "Version is required.";
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

        public string Description
        {
            get { return _description; }
            set
            {
                if (String.IsNullOrWhiteSpace(value))
                {
                    string message = "Description is required.";
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

        IEnumerable<string> IPackageMetadata.Authors
        {
            get { return SplitString(Authors); }
        }

        IEnumerable<string> IPackageMetadata.Owners
        {
            get { return SplitString(Owners); }
        }

        IEnumerable<PackageDependency> IPackageMetadata.Dependencies
        {
            get { return Dependencies; }
        }

        IEnumerable<FrameworkAssemblyReference> IPackageMetadata.FrameworkAssemblies
        {
            get { return FrameworkAssemblies; }
        }

        IEnumerable<AssemblyReference> IPackageMetadata.References
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
            Description = source.Description;
            Summary = source.Summary;
            ReleaseNotes = source.ReleaseNotes;
            Copyright = source.Copyright;
            Language = source.Language;
            Tags = source.Tags;
            Dependencies = new ObservableCollection<PackageDependency>(source.Dependencies);
            FrameworkAssemblies = new ObservableCollection<FrameworkAssemblyReference>(source.FrameworkAssemblies);
            PackageAssemblyReferences = new ObservableCollection<AssemblyReference>();
            if (source.References != null)
            {
                PackageAssemblyReferences.AddRange(source.References);
            }
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
                    string message = "Enabling license acceptance requires a license url.";
                    return message;
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

        public bool ContainsAssemblyReference(string name)
        {
            return PackageAssemblyReferences.Any(f => f.File.Equals(name, StringComparison.OrdinalIgnoreCase));
        }

        public void AddAssemblyReference(string name)
        {
            PackageAssemblyReferences.Add(new AssemblyReference(name));
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