using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.Versioning;
using NuGet;

namespace PackageExplorerViewModel
{
    public class EditablePackageDependencySet : INotifyPropertyChanged
    {
        private FrameworkName _targetFramework;
        private ObservableCollection<PackageDependency> _dependencies;

        public EditablePackageDependencySet()
        {
            _dependencies = new ObservableCollection<PackageDependency>();
        }

        public EditablePackageDependencySet(PackageDependencySet packageDependencySet)
        {
            _targetFramework = packageDependencySet.TargetFramework;
            _dependencies = new ObservableCollection<PackageDependency>(packageDependencySet.Dependencies);
        }

        public FrameworkName TargetFramework
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

        public ObservableCollection<PackageDependency> Dependencies
        {
            get
            {
                return _dependencies;
            }
        }

        public PackageDependencySet AsReadOnly()
        {
            return new PackageDependencySet(TargetFramework, Dependencies);
        }

        public event PropertyChangedEventHandler PropertyChanged;

        private void OnPropertyChange(string propertyName)
        {
            if (PropertyChanged != null)
            {
                PropertyChanged(this, new PropertyChangedEventArgs(propertyName));
            }
        }
    }
}
