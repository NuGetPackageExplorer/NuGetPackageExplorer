using System.ComponentModel;
using PackageExplorerViewModel;

namespace NuGetPackageExplorer.Types {
    public sealed class FileContentInfo : INotifyPropertyChanged {
        public FileContentInfo(PackageFile file, string name, object content, bool isTextFile, long size, SourceLanguageType language) {
            File = file;
            Name = name;
            Content = content;
            IsTextFile = isTextFile;
            Size = size;
            Language = language;
        }

        public PackageFile File { get; set; }
        public string Name { get; private set; }
        public object Content { get; private set; }
        public bool IsTextFile { get; private set; }
        public long Size { get; private set; }

        private SourceLanguageType _language;

        public SourceLanguageType Language {
            get { return _language; }
            set {
                if (_language != value) {
                    _language = value;
                    OnPropertyChanged("Language");
                }
            }
        }

        private void OnPropertyChanged(string name) {
            if (PropertyChanged != null) {
                PropertyChanged(this, new PropertyChangedEventArgs(name));
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;
    }
}