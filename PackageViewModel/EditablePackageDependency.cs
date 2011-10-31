using System;
using System.ComponentModel;
using NuGet;

namespace PackageExplorerViewModel
{
    public class EditablePackageDependency : INotifyPropertyChanged, IDataErrorInfo
    {
        private string _id;
        private IVersionSpec _versionSpec;

        public EditablePackageDependency()
        {
        }

        public EditablePackageDependency(string id, IVersionSpec versionSpec)
        {
            Id = id;
            VersionSpec = versionSpec;
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

        public IVersionSpec VersionSpec
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
                    return null;
                }

                if (!PackageIdValidator.IsValidPackageId(Id))
                {
                    return "Value '" + Id + "' cannot be converted.";
                }
            }

            return null;
        }

        public PackageDependency AsReadOnly()
        {
            return new PackageDependency(Id, VersionSpec);
        }
    }
}