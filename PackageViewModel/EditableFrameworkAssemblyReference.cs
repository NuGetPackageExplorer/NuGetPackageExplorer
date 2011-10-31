using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Runtime.Versioning;
using NuGet;

namespace PackageExplorerViewModel
{
    public class EditableFrameworkAssemblyReference : INotifyPropertyChanged
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
    }
}