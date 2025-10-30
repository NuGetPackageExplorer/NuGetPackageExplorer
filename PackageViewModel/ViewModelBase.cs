using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace PackageExplorerViewModel
{
    public abstract class ViewModelBase : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler? PropertyChanged = static delegate { };

        protected virtual void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        {
            PropertyChanged!(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
