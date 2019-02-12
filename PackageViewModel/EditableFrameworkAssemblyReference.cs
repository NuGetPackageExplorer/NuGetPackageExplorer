using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using NuGet.Frameworks;
using NuGet.Packaging;

namespace PackageExplorerViewModel
{
    public class EditableFrameworkAssemblyReference : INotifyPropertyChanged, IDataErrorInfo
    {
        private string? _assemblyName;

        private IEnumerable<NuGetFramework>? _supportedFrameworks;

        public string? AssemblyName
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

        public IEnumerable<NuGetFramework> SupportedFrameworks
        {
            get { return _supportedFrameworks ?? Enumerable.Empty<NuGetFramework>(); }
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
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        public FrameworkAssemblyReference AsReadOnly()
        {
            return new FrameworkAssemblyReference(AssemblyName, SupportedFrameworks);
        }

        public string? Error
        {
            get { return null; }
        }

        public string? this[string columnName]
        {
            get
            {
                if (columnName == "AssemblyName")
                {
                    if (string.IsNullOrEmpty(AssemblyName))
                    {
                        return _supportedFrameworks == null ? null : "Assembly name must not be empty.";
                    }
                }

                return null;
            }
        }
    }
}
