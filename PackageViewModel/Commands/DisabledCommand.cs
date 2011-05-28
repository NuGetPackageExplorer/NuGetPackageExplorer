using System;
using System.Windows.Input;

namespace PackageExplorerViewModel {
    public sealed class DisabledCommand : ICommand {
        public bool CanExecute(object parameter) {
            return false;
        }

        event EventHandler ICommand.CanExecuteChanged {
            add { }
            remove { }
        }

        public void Execute(object parameter) {
            throw new NotSupportedException();
        }
    }
}