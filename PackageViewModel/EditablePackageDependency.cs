using System;
using System.Linq;
using System.ComponentModel;
using System.Diagnostics;
using NuGetPe;

namespace PackageExplorerViewModel
{
    public class EditablePackageDependency : INotifyPropertyChanged, IDataErrorInfo
    {
        private string _id;
        private string _exclude;
        private NuGet.IVersionSpec _versionSpec;
        private Func<EditablePackageDependencySet> _getActiveDependencySet;

        public EditablePackageDependency(Func<EditablePackageDependencySet> getActiveDependencySet)
        {
            Debug.Assert(getActiveDependencySet != null);
            _getActiveDependencySet = getActiveDependencySet;
        }

        public string Id
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

        public NuGet.IVersionSpec VersionSpec
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

        public string Exclude
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

        private void RaisePropertyChange(string propertyName)
        {
            if (PropertyChanged != null)
            {
                PropertyChanged(this, new PropertyChangedEventArgs(propertyName));
            }
        }

        private string IsValid(string columnName)
        {
            if (columnName == "Id")
            {
                if (String.IsNullOrEmpty(Id))
                {
                    return VersionSpec != null ? "Package id must not be empty." : (string)null;
                }

                if (!NuGet.PackageIdValidator.IsValidPackageId(Id))
                {
                    return "'" + Id + "' is an invalid package id.";
                }

                EditablePackageDependencySet activeDependencySet = _getActiveDependencySet();
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
            return new PackageDependency(Id, VersionSpec, Exclude);
        }
    }
}