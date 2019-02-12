using System.Collections.ObjectModel;
using System.ComponentModel;
using NuGet.Frameworks;
using NuGet.Packaging;

namespace PackageExplorerViewModel
{
    public class EditablePackageReferenceSet : INotifyPropertyChanged
    {
        private NuGetFramework? _targetFramework;

        public EditablePackageReferenceSet()
        {
            References = new ObservableCollection<string>();
        }

        public EditablePackageReferenceSet(PackageReferenceSet packageReferenceSet)
        {
            _targetFramework = packageReferenceSet.TargetFramework;
            References = new ObservableCollection<string>(packageReferenceSet.References);
        }

        public event PropertyChangedEventHandler PropertyChanged;

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

        public PackageReferenceSet AsReadOnly()
        {
            return new PackageReferenceSet(TargetFramework, References);
        }

        public ObservableCollection<string> References { get; }

        private void OnPropertyChange(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
