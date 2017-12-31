using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Linq;
using System.Windows.Input;
using NuGetPackageExplorer.Types;

namespace PackageExplorerViewModel
{
    internal class ViewContentCommand : CommandBase, ICommand
    {
        public ViewContentCommand(PackageViewModel packageViewModel)
            : base(packageViewModel)
        {
        }

        #region ICommand Members

        public event EventHandler CanExecuteChanged = delegate { };

        public bool CanExecute(object parameter)
        {
            if (ViewModel.IsInEditFileMode)
            {
                return false;
            }

            if ("Hide".Equals(parameter))
            {
                return true;
            }
            else
            {
                return ViewModel.SelectedItem is PackageFile;
            }
        }

        public void Execute(object parameter)
        {
            if ("Hide".Equals(parameter))
            {
                ViewModel.ShowContentViewer = false;
            }
            else
            {
                if ((parameter ?? ViewModel.SelectedItem) is PackageFile file)
                {
                    ShowFile(file);
                }
            }
        }

        #endregion

        public void RaiseCanExecuteChanged()
        {
            CanExecuteChanged(this, EventArgs.Empty);
        }

        [SuppressMessage(
            "Microsoft.Design",
            "CA1031:DoNotCatchGeneralExceptionTypes",
            Justification = "We don't want plugin to crash the app.")]
        private void ShowFile(PackageFile file)
        {
            object content = null;
            var isBinary = false;

            // find all plugins which can handle this file's extension
            var contentViewers = FindContentViewer(file);
            if (contentViewers != null)
            {
                isBinary = true;
                try
                {
                    // iterate over all plugins, looking for the first one that return non-null content
                    foreach (var viewer in contentViewers)
                    {
                        using (var stream = file.GetStream())
                        {
                            content = viewer.GetView(Path.GetExtension(file.Name), stream);
                            if (content != null)
                            {
                                // found a plugin that can read this file, stop
                                break;
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    // don't let plugin crash the app
                    content = Resources.PluginFailToReadContent + Environment.NewLine + ex.ToString();
                }

                if (content is string)
                {
                    isBinary = false;
                }
            }

            // if plugins fail to read this file, fall back to the default viewer
            long size = -1;
            if (content == null)
            {
                isBinary = FileHelper.IsBinaryFile(file.Name);
                if (isBinary)
                {
                   content = Resources.UnsupportedFormatMessage;
                }
                else
                {
                    content = ReadFileContent(file, out size);
                }
            }

            if (size == -1)
            { 
                // This is inefficient but cn be cleaned up later
                using (var str = file.GetStream())
                using (var ms = new MemoryStream())
                {
                    str.CopyTo(ms);
                    size = ms.Length;
                }
            }

            var fileInfo = new FileContentInfo(
                file,
                file.Path,
                content,
                !isBinary,
                size);

            ViewModel.ShowFile(fileInfo);
        }

        private IEnumerable<IPackageContentViewer> FindContentViewer(PackageFile file)
        {
            var extension = Path.GetExtension(file.Name);
            return from p in ViewModel.ContentViewerMetadata
                   where p.Metadata.SupportedExtensions.Contains(extension, StringComparer.OrdinalIgnoreCase)
                   orderby p.Metadata.Priority
                   select p.Value;
        }

        private static string ReadFileContent(PackageFile file, out long size)
        {
            using (var stream = file.GetStream())
            using(var ms = new MemoryStream())
            using (var reader = new StreamReader(ms))
            {
                stream.CopyTo(ms);
                size = ms.Length;
                ms.Position = 0;

                return reader.ReadToEnd();
            }
        }
    }
}