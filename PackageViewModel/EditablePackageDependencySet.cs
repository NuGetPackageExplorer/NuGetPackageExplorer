using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.Versioning;
using NuGet.Frameworks;
using NuGet.Packaging.Core;
using NuGetPe;

namespace PackageExplorerViewModel
{
    public class EditablePackageDependencySet : INotifyPropertyChanged
    {
        private NuGetFramework _targetFramework;

        public EditablePackageDependencySet()
        {
            Dependencies = new ObservableCollection<PackageDependency>();
        }

        public EditablePackageDependencySet(PackageDependencySet packageDependencySet)
        {
            _targetFramework = packageDependencySet.TargetFramework;
            Dependencies = new ObservableCollection<PackageDependency>(packageDependencySet.Dependencies);
        }

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

        public ObservableCollection<PackageDependency> Dependencies
        {
            get; }

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
