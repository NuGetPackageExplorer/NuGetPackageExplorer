using System.Collections.Generic;
using System.ComponentModel;
using System.Runtime.Versioning;
using NuGet;

namespace PackageExplorerViewModel {

    public class EditableFrameworkAssemblyReference : INotifyPropertyChanged {

        public EditableFrameworkAssemblyReference() {
        }

        public EditableFrameworkAssemblyReference(string assemblyName, IEnumerable<FrameworkName> supportedFrameworks) {
            this.AssemblyName = assemblyName;
            this.SupportedFrameworks = supportedFrameworks;
        }

        private string _assemblyName;

        public string AssemblyName {
            get {
                return _assemblyName;
            }
            set {
                if (_assemblyName != value) {
                    _assemblyName = value;
                    RaisePropertyChange("AssemblyName");
                }
            }
        }

        private IEnumerable<FrameworkName> _supportedFrameworks;

        public IEnumerable<FrameworkName> SupportedFrameworks {
            get { return _supportedFrameworks; }
            set {
                if (_supportedFrameworks != value) {
                    _supportedFrameworks = value;
                    RaisePropertyChange("SupportedFrameworks");
                }
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;

        private void RaisePropertyChange(string propertyName) {
            if (PropertyChanged != null) {
                PropertyChanged(this, new PropertyChangedEventArgs(propertyName));
            }
        }

        public FrameworkAssemblyReference AsReadOnly() {
            return new FrameworkAssemblyReference(AssemblyName, SupportedFrameworks);
        }
    }
}