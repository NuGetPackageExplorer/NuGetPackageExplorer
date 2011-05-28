using System.ComponentModel;

namespace PackageExplorerViewModel {
    public abstract class ViewModelBase : INotifyPropertyChanged {

        protected ViewModelBase() {
        }

        public event PropertyChangedEventHandler PropertyChanged;

        protected void OnPropertyChanged(string propertyName) {
            if (PropertyChanged != null) {
                PropertyChanged(this, new PropertyChangedEventArgs(propertyName));
            }
        }
    }
}
