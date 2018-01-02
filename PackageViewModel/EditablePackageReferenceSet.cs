using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.Versioning;
using NuGet.Frameworks;
using NuGet.Packaging;
using NuGetPe;

namespace PackageExplorerViewModel
{
    public class EditablePackageReferenceSet : INotifyPropertyChanged
    {
        private NuGetFramework _targetFramework;
        private ObservableCollection<string> _references;

        public EditablePackageReferenceSet()
        {
            _references = new ObservableCollection<string>();
        }

        public EditablePackageReferenceSet(PackageReferenceSet packageReferenceSet)
        {
            _targetFramework = packageReferenceSet.TargetFramework;
            _references = new ObservableCollection<string>(packageReferenceSet.References);
        }

        public event PropertyChangedEventHandler PropertyChanged;

        public NuGetFramework TargetFramework
        {
            get
            {
                return _targetFramework;
            }
            set
            {
                if (_targetFramework != value)
                {
                    _targetFramework = value;
                    OnPropertyChange("TargetFramework");
                }
            }
        }

        public PackageReferenceSet AsReadOnly()
        {
            return new PackageReferenceSet(TargetFramework, References);
        }

        public ObservableCollection<string> References
        {
            get
            {
                return _references;
            }
        }

        private void OnPropertyChange(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}