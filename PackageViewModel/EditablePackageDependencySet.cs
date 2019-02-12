using System.Collections.ObjectModel;
using System.ComponentModel;
using NuGet.Frameworks;
using NuGet.Packaging;
using NuGet.Packaging.Core;

namespace PackageExplorerViewModel
{
    public class EditablePackageDependencySet : INotifyPropertyChanged
    {
        private NuGetFramework? _targetFramework;

        public EditablePackageDependencySet()
        {
            Dependencies = new ObservableCollection<PackageDependency>();
        }

        public EditablePackageDependencySet(PackageDependencyGroup packageDependencySet)
        {
            _targetFramework = packageDependencySet.TargetFramework;
            Dependencies = new ObservableCollection<PackageDependency>(packageDependencySet.Packages);
        }

        public NuGetFramework? TargetFramework
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
            get;
        }

        public PackageDependencyGroup AsReadOnly()
        {
            return new PackageDependencyGroup(TargetFramework ?? NuGetFramework.AnyFramework, Dependencies);
        }

        public event PropertyChangedEventHandler PropertyChanged;

        private void OnPropertyChange(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
