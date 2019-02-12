using System;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using NuGet.Packaging;
using NuGet.Packaging.Core;
using NuGet.Versioning;

namespace PackageExplorerViewModel
{
    public class EditablePackageDependency : INotifyPropertyChanged, IDataErrorInfo
    {
        private string? _id;
        private string? _exclude;
        private VersionRange? _versionSpec;
        private readonly Func<EditablePackageDependencySet> _getActiveDependencySet;

        public EditablePackageDependency(Func<EditablePackageDependencySet> getActiveDependencySet)
        {
            Debug.Assert(getActiveDependencySet != null);
            _getActiveDependencySet = getActiveDependencySet;
        }

        public string? Id
        {
            get { return _id; }
            set
            {
                if (_id != value)
                {
                    _id = value;
                    RaisePropertyChange("Id");
                }
            }
        }

        public VersionRange? VersionSpec
        {
            get { return _versionSpec; }
            set
            {
                if (_versionSpec != value)
                {
                    _versionSpec = value;
                    RaisePropertyChange("VersionSpec");
                }
            }
        }

        public string? Exclude
        {
            get { return _exclude; }
            set
            {
                if (_exclude != value)
                {
                    _exclude = value;
                    RaisePropertyChange("Exclude");
                }
            }
        }

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

        #endregion

        private void RaisePropertyChange(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        private string? IsValid(string columnName)
        {
            if (columnName == "Id")
            {
                if (string.IsNullOrEmpty(Id))
                {
                    return VersionSpec != null ? "Package id must not be empty." : null;
                }

                if (!PackageIdValidator.IsValidPackageId(Id))
                {
                    return "'" + Id + "' is an invalid package id.";
                }

                var activeDependencySet = _getActiveDependencySet();
                if (activeDependencySet != null)
                {
                    if (activeDependencySet.Dependencies.Any(p => p.Id.Equals(Id, StringComparison.OrdinalIgnoreCase)))
                    {
                        return "This id already exists in the same dependency group.";
                    }
                }
            }

            return null;
        }

        public PackageDependency AsReadOnly()
        {
            return new PackageDependency(Id, VersionSpec, null, Exclude?.Split(',').Select(s => s.Trim()).ToList());
        }
    }
}
