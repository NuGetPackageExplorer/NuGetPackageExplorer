using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Windows.Input;
using NuGet.Packaging;
using NuGetPackageExplorer.Types;

namespace PackageExplorerViewModel
{
    [SuppressMessage("Microsoft.Design", "CA1036:OverrideMethodsOnComparableTypes")]
    public abstract class PackagePart : IComparable<PackagePart>, INotifyPropertyChanged, IDisposable
    {
        private int _hashCode;
        private bool _isSelected;
        private string? _name;
        private PackageFolder? _parent;
        private string _path;
        private string? _extension;

#pragma warning disable CS8618 // Non-nullable field is uninitialized.
        protected PackagePart(string name, PackageFolder? parent, PackageViewModel viewModel)
#pragma warning restore CS8618 // Non-nullable field is uninitialized.
        {
            if (name == null)
            {
                throw new ArgumentNullException("name");
            }

            PackageViewModel = viewModel ?? throw new ArgumentNullException("viewModel");
            _parent = parent;

            OnNameChange(name);
            RecalculatePath();
        }

        public PackageViewModel PackageViewModel { get; }

        public PackageFolder? Parent
        {
            get { return _parent; }
            internal set
            {
                if (_parent != value)
                {
                    _parent = value;
                    UpdatePath();
                }
            }
        }

        public string Name
        {
            get { return _name!; }
            set
            {
                if (value == null)
                    throw new ArgumentNullException(nameof(value));

                if (_name != value)
                {
                    OnNameChange(value);
                    UpdatePath();
                }
            }
        }

        private void OnNameChange(string newName)
        {
            // precalculate hash code to improve perf
            _hashCode = newName == null ? 0 : newName.ToUpperInvariant().GetHashCode();

            _name = newName;
            OnPropertyChanged("Name");

            Extension = newName == null ? null : System.IO.Path.GetExtension(newName);
        }

        public string? Extension
        {
            get { return _extension; }
            set
            {
                if (_extension != value)
                {
                    _extension = value;
                    OnPropertyChanged("Extension");
                }
            }
        }

        public string Path
        {
            get { return _path; }
            set
            {
                if (_path != value)
                {
                    _path = value;
                    OnPropertyChanged("Path");
                }
            }
        }

        public bool IsSelected
        {
            get { return _isSelected; }
            set
            {
                if (_isSelected != value)
                {
                    _isSelected = value;
                    OnPropertyChanged("IsSelected");
                }
            }
        }

        public ICommand DeleteCommand
        {
            get { return PackageViewModel.DeleteContentCommand; }
        }

        public ICommand RenameCommand
        {
            get { return PackageViewModel.RenameContentCommand; }
        }

        #region IComparable<PackagePart> Members

        public int CompareTo(PackagePart other)
        {
            if (this == other)
            {
                return 0;
            }

            if (other == null)
            {
                return 1;
            }

            // folder goes before file
            if (this is PackageFolder && other is PackageFile)
            {
                return -1;
            }

            if (this is PackageFile && other is PackageFolder)
            {
                return 1;
            }

            return string.Compare(Path, other.Path, StringComparison.OrdinalIgnoreCase);
        }

        #endregion

        #region IDisposable Members

        public void Dispose()
        {
            try
            {
                Dispose(true);
            }
            finally
            {
                GC.SuppressFinalize(this);
            }
        }

        #endregion

        #region INotifyPropertyChanged Members

        public event PropertyChangedEventHandler PropertyChanged;

        #endregion

        public abstract void Export(string rootPath);

        public void Rename(string newName)
        {
            if (!Name.Equals(newName, StringComparison.Ordinal))
            {
                if (Parent != null)
                {
                    if (!Name.Equals(newName, StringComparison.OrdinalIgnoreCase) &&
                        (Parent.ContainsFile(newName) || Parent.ContainsFolder(newName)))
                    {
                        PackageViewModel.UIServices.Show(
                            string.Format(CultureInfo.CurrentCulture, Resources.RenameCausesNameCollison, newName),
                            MessageLevel.Error);
                        return;
                    }
                }

                Name = newName;
                PackageViewModel.NotifyContentRenamed(this);
            }
        }

        public void Delete(bool requireConfirmation = true)
        {
            if (requireConfirmation)
            {
                var confirm = PackageViewModel.UIServices.Confirm(
                    Resources.ConfirmToDeleteContent_Title,
                    string.Format(CultureInfo.CurrentCulture, Resources.ConfirmToDeleteContent, Name),
                    isWarning: true);

                if (!confirm)
                {
                    return;
                }
            }

            if (Parent != null)
            {
                Parent.RemoveChild(this);
                PackageViewModel.NotifyContentDeleted(this);
            }
        }

        public bool IsDescendantOf(PackagePart container)
        {
            if (container == this)
            {
                return true;
            }

            if (container is PackageFile)
            {
                // a file cannot be an ancestor of another part
                return false;
            }

            for (PackagePart? cursor = this; cursor != null; cursor = cursor.Parent)
            {
                if (cursor == container)
                {
                    return true;
                }
            }

            return false;
        }

        [SuppressMessage(
            "Microsoft.Design",
            "CA1024:UsePropertiesWhereAppropriate",
            Justification = "This method is potentially expensive.")]
        public abstract IEnumerable<IPackageFile> GetFiles();

        [SuppressMessage(
            "Microsoft.Design",
            "CA1024:UsePropertiesWhereAppropriate",
            Justification = "This method is potentially expensive.")]
        public abstract IEnumerable<PackagePart> GetPackageParts();

        protected void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        protected void RecalculatePath()
        {
            Path = (Parent == null || string.IsNullOrEmpty(Parent.Path)) ? Name : (Parent.Path + "\\" + Name);
        }

        internal virtual void UpdatePath()
        {
            RecalculatePath();
        }

        public override bool Equals(object obj)
        {
            if (!(obj is PackagePart other))
            {
                return false;
            }

            return CompareTo(other) == 0;
        }

        public override int GetHashCode()
        {
            return _hashCode;
        }

        protected virtual void Dispose(bool disposing)
        {
        }

        ~PackagePart()
        {
            Dispose(false);
        }
    }
}
