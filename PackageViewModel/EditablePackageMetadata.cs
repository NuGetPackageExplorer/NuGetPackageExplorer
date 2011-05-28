using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using NuGet;

namespace PackageExplorerViewModel {

    public class EditablePackageMetadata : IPackageMetadata, IDataErrorInfo, INotifyPropertyChanged {

        private readonly Dictionary<string, string> _propertyErrors = new Dictionary<string, string>();

        public EditablePackageMetadata() {
        }

        public EditablePackageMetadata(IPackageMetadata source) {
            CopyFrom(source);
        }

        public void CopyFrom(IPackageMetadata source) {
            this.Id = source.Id;
            this.Version = source.Version;
            this.Title = source.Title;
            this.Authors = ConvertToString(source.Authors);
            this.Owners = ConvertToString(source.Owners);
            this.IconUrl = FixIconUrl(source.IconUrl);
            this.LicenseUrl = source.LicenseUrl;
            this.ProjectUrl = source.ProjectUrl;
            this.RequireLicenseAcceptance = source.RequireLicenseAcceptance;
            this.Description = source.Description;
            this.Summary = source.Summary;
            this.Language = source.Language;
            this.Tags = source.Tags;
            this.Dependencies = new ObservableCollection<PackageDependency>(source.Dependencies);
            this.FrameworkAssemblies = new ObservableCollection<FrameworkAssemblyReference>(source.FrameworkAssemblies);
        }

        private static Uri FixIconUrl(Uri uri)
        {
            if (uri == null || uri.IsAbsoluteUri)
            {
                return uri;
            }

            string path = uri.OriginalString;
            if (path.StartsWith("//", StringComparison.Ordinal)) {
                path = path.Substring(1);
            }

            UriBuilder builder = new UriBuilder()
            {
                Scheme = "http",
                Host = "www.nuget.org",
                Path = path
            };

            return builder.Uri;
        }

        private string _id;

        public string Id { 
            get {
                return _id;
            }
            set {
                try {
                    if (String.IsNullOrWhiteSpace(value)) {
                        throw new ArgumentException("Id is required.");
                    }

                    PackageIdValidator.ValidatePackageId(value);
                }
                catch (Exception ex) {
                    SetError("Id", ex.Message);
                    throw;
                }

                SetError("Id", null);
                if (_id != value) {
                    _id = value;
                    RaisePropertyChange("Id");
                }
            }
        }

        private Version _version;
        public Version Version {
            get { return _version; }
            set {
                if (value == null) {
                    string message = "Version is required.";
                    SetError("Version", message);
                    throw new ArgumentException(message);
                }

                SetError("Version", null);
                if (_version != value) {
                    _version = value;
                    RaisePropertyChange("Version");
                }
            }
        }

        private string _title;

        public string Title {
            get {
                return _title;
            }
            set {
                if (_title != value) {
                    _title = value;
                    RaisePropertyChange("Title");
                }
            }
        }

        private string _authors;

        public string Authors 
        {
            get { return _authors; }
            set {
                if (String.IsNullOrWhiteSpace(value)) {
                    string message = "Authors is required.";
                    SetError("Authors", message);
                    throw new ArgumentException(message);
                }

                SetError("Authors", null);
                if (_authors != value) {
                    _authors = value;
                    RaisePropertyChange("Authors");
                }
            }
        }

        private string _owners;

        public string Owners {
            get {
                return _owners;
            }
            set {
                if (_owners != value) {
                    _owners = value;
                    RaisePropertyChange("Owners");
                }
            }
        }

        private Uri _iconUrl;

        public Uri IconUrl {
            get {
                return _iconUrl;
            }
            set {
                if (_iconUrl != value) {
                    _iconUrl = value;
                    RaisePropertyChange("IconUrl");
                }
            }
        }

        private Uri _licenseUrl;

        public Uri LicenseUrl {
            get {
                return _licenseUrl;
            }
            set {
                if (_licenseUrl != value) {
                    _licenseUrl = value;
                    RaisePropertyChange("LicenseUrl");
                }
            }
        }

        private Uri _projectUrl;

        public Uri ProjectUrl {
            get {
                return _projectUrl;
            }
            set {
                if (_projectUrl != value) {
                    _projectUrl = value;
                    RaisePropertyChange("ProjectUrl");
                }
            }
        }
        
        private bool _requireLicenseAcceptance; 
        public bool RequireLicenseAcceptance {
            get { return _requireLicenseAcceptance; }
            set {
                if (value != _requireLicenseAcceptance) {
                    _requireLicenseAcceptance = value;
                    RaisePropertyChange("RequireLicenseAcceptance");
                    RaisePropertyChange("LicenseUrl");
                }
            }
        }

        private string _description;

        public string Description {
            get { return _description; }
            set {
                if (String.IsNullOrWhiteSpace(value)) {
                    string message = "Description is required.";
                    SetError("Description", message);
                    throw new ArgumentException(message);
                }

                SetError("Description", null);

                if (_description != value) {
                    _description = value;
                    RaisePropertyChange("Description");
                }
            }
        }

        private string _summary;

        public string Summary {
            get {
                return _summary;
            }
            set {
                if (_summary != value) {
                    _summary = value;
                    RaisePropertyChange("Summary");
                }
            }
        }

        private string _language;

        public string Language {
            get {
                return _language;
            }
            set {
                if (_language != value) {
                    _language = value;
                    RaisePropertyChange("Language");
                }
            }
        }

        private string _tags;

        public string Tags {
            get {
                return _tags;
            }
            set {
                if (_tags != value) {
                    _tags = value;
                    RaisePropertyChange("Tags");
                }
            }
        }

        public ObservableCollection<PackageDependency> Dependencies { get; private set; }

        public ObservableCollection<FrameworkAssemblyReference> FrameworkAssemblies { get; private set; }

        IEnumerable<string> IPackageMetadata.Authors {
            get {
                return SplitString(this.Authors);
            }
        }

        IEnumerable<string> IPackageMetadata.Owners {
            get {
                return SplitString(this.Owners);
            }
        }

        private static IEnumerable<string> SplitString(string text) {
            return text == null ? Enumerable.Empty<string>() : text.Split(',').Select(a => a.Trim());
        }

        private static string ConvertToString(IEnumerable<string> items) {
            return String.Join(", ", items);
        }

        IEnumerable<PackageDependency> IPackageMetadata.Dependencies {
            get {
                return this.Dependencies;
            }
        }

        IEnumerable<FrameworkAssemblyReference> IPackageMetadata.FrameworkAssemblies {
            get {
                return this.FrameworkAssemblies;
            }
        }

        public override string ToString() {
            return Id + "." + Version.ToString();
        }

        public string Error {
            get {
                return null;
            }
        }

        public string this[string columnName] {
            get { return IsValid(columnName); }
        }

        private string IsValid(string propertyName) {

            if (propertyName == "LicenseUrl") {
                if (RequireLicenseAcceptance && LicenseUrl == null) {
                    string message = "Enabling license acceptance requires a license url.";
                    return message;
                }
            }

            string error;
            _propertyErrors.TryGetValue(propertyName, out error);
            return error;
        }

        private void SetError(string property, string error) {
            if (String.IsNullOrEmpty(error)) {
                _propertyErrors.Remove(property);
            }
            else {
                _propertyErrors[property] = error;
            }
        }

        public void ResetErrors() {
            _propertyErrors.Clear();
        }

        public event PropertyChangedEventHandler PropertyChanged;

        private void RaisePropertyChange(string propertyName) {
            if (PropertyChanged != null) {
                PropertyChanged(this, new PropertyChangedEventArgs(propertyName));
            }
        }
    }
}