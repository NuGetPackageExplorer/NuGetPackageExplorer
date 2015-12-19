using System;
using System.Windows.Input;

namespace PackageExplorerViewModel
{
    public sealed class DisabledCommand : ICommand
    {
        #region ICommand Members

        public bool CanExecute(object parameter)
        {
            return false;
        }

        event EventHandler ICommand.CanExecuteChanged
        {
            add { }
            remove { }
        }

        public void Execute(object parameter)
        {
        }

        #endregion
    }
}