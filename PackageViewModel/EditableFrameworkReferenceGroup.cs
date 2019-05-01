using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using NuGet.Frameworks;
using NuGet.Packaging;

namespace PackageExplorerViewModel
{
    public class EditableFrameworkReferenceGroup : INotifyPropertyChanged
    {
        private NuGetFramework? _targetFramework;

        public EditableFrameworkReferenceGroup()
        {
            FrameworkReferences = new ObservableCollection<string>();
        }

        public EditableFrameworkReferenceGroup(FrameworkReferenceGroup frameworkReferenceGroup)
        {
            _targetFramework = frameworkReferenceGroup.TargetFramework;
            FrameworkReferences = new ObservableCollection<string>(frameworkReferenceGroup.FrameworkReferences.Select(fr => fr.Name));
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
                    OnPropertyChange(nameof(TargetFramework));
                }
            }
        }

        public FrameworkReferenceGroup AsReadOnly()
        {
            return new FrameworkReferenceGroup(TargetFramework, FrameworkReferences.Select(fr => new FrameworkReference(fr)));
        }

        public ObservableCollection<string> FrameworkReferences { get; }

        private void OnPropertyChange(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
