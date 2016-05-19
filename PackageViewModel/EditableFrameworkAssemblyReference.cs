using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Runtime.Versioning;
using NuGetPe;

namespace PackageExplorerViewModel
{
    public class EditableFrameworkAssemblyReference : INotifyPropertyChanged, IDataErrorInfo
    {
        private string _assemblyName;

        private IEnumerable<FrameworkName> _supportedFrameworks;

        public string AssemblyName
        {
            get { return _assemblyName; }
            set
            {
                if (_assemblyName != value)
                {
                    _assemblyName = value;
                    RaisePropertyChange("AssemblyName");
                }
            }
        }

        public IEnumerable<FrameworkName> SupportedFrameworks
        {
            get { return _supportedFrameworks ?? Enumerable.Empty<FrameworkName>(); }
            set
            {
                if (_supportedFrameworks != value)
                {
                    _supportedFrameworks = value;
                    RaisePropertyChange("SupportedFrameworks");
                    RaisePropertyChange("AssemblyName");
                }
            }
        }

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

        public FrameworkAssemblyReference AsReadOnly(string displayValue)
        {
            return new FrameworkAssemblyReference(AssemblyName, SupportedFrameworks, displayValue);
        }

        public string Error
        {
            get { return null; }
        }

        public string this[string columnName]
        {
            get
            {
                if (columnName == "AssemblyName")
                {
                    if (String.IsNullOrEmpty(AssemblyName))
                    {
                        return _supportedFrameworks == null ? (string)null : "Assembly name must not be empty.";
                    }
                }

                return null;
            }
        }
    }
}