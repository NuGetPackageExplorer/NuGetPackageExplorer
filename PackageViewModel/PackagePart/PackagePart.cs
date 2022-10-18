using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Windows.Input;
using NuGet.Packaging;
using NuGetPackageExplorer.Types;

using NuGetPe;

namespace PackageExplorerViewModel
{
    [SuppressMessage("Microsoft.Design", "CA1036:OverrideMethodsOnComparableTypes")]
    public abstract class PackagePart : IPart, IComparable<PackagePart>, INotifyPropertyChanged, IDisposable
    {
        private int _hashCode;
        private bool _isSelected;
        private string? _name;
        private string _path;
        private string? _extension;
        protected PackageFolder? _parent;

#pragma warning disable CS8618 // Non-nullable field is uninitialized.
        protected PackagePart(string name, PackageFolder? parent, PackageViewModel? viewModel)
#pragma warning restore CS8618 // Non-nullable field is uninitialized.
        {
            if (name == null)
            {
                throw new ArgumentNullException(nameof(name));
            }

            PackageViewModel = viewModel;
            _parent = parent;

            OnNameChange(name);
            RecalculatePath();
        }

        public PackageViewModel? PackageViewModel { get; }

        public IFolder? Parent
        {
            get { return _parent; }
            internal set
            {
                if (_parent != value)
                {
                    _parent = (PackageFolder?) value;
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
            _hashCode = newName == null ? 0 : newName.ToUpperInvariant().GetHashCode(StringComparison.InvariantCulture);

            _name = newName;
            OnPropertyChanged(nameof(Name));

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
                    OnPropertyChanged(nameof(Extension));
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
                    OnPropertyChanged(nameof(Path));
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
                    OnPropertyChanged(nameof(IsSelected));
                }
            }
        }

        public ICommand? DeleteCommand
        {
            get { return PackageViewModel?.DeleteContentCommand; }
        }

        public ICommand? RenameCommand
        {
            get { return PackageViewModel?.RenameContentCommand; }
        }


        public int CompareTo(PackagePart? other)
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


        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }



        public event PropertyChangedEventHandler? PropertyChanged;

        public abstract void Export(string rootPath);

        public void Rename(string newName)
        {
            if (!Name.Equals(newName, StringComparison.Ordinal))
            {
                if (_parent != null)
                {
                    if (!Name.Equals(newName, StringComparison.OrdinalIgnoreCase) &&
                        (_parent.ContainsFile(newName) || _parent.ContainsFolder(newName)))
                    {
                        PackageViewModel?.UIServices.Show(
                            string.Format(CultureInfo.CurrentCulture, Resources.RenameCausesNameCollison, newName),
                            MessageLevel.Error);
                        return;
                    }
                }

                Name = newName;
                PackageViewModel?.NotifyContentRenamed(this);
            }
        }

        public void Delete(bool requireConfirmation = true)
        {
            if (requireConfirmation)
            {
                var confirm = PackageViewModel?.UIServices.Confirm(
                    Resources.ConfirmToDeleteContent_Title,
                    string.Format(CultureInfo.CurrentCulture, Resources.ConfirmToDeleteContent, Name),
                    isWarning: true) ?? true; // no confirm of non-UI code

                if (!confirm)
                {
                    return;
                }
            }

            if (_parent != null)
            {
                _parent.RemoveChild(this);
                PackageViewModel?.NotifyContentDeleted(this);
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

            for (PackagePart? cursor = this; cursor != null; cursor = cursor._parent)
            {
                if (cursor == container)
                {
                    return true;
                }
            }

            return false;
        }

        public abstract IEnumerable<IFile> GetFiles();

        public abstract IEnumerable<IPackageFile> GetPackageFiles();

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

        public override bool Equals(object? obj)
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
