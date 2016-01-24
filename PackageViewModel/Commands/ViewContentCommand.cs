using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Linq;
using System.Text;
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
                var file = (parameter ?? ViewModel.SelectedItem) as PackageFile;
                if (file != null)
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
            long size = -1;
            object content = null;
            bool isBinary = false;

            // find all plugins which can handle this file's extension
            IEnumerable<IPackageContentViewer> contentViewers = FindContentViewer(file);
            if (contentViewers != null)
            {
                isBinary = true;
                try
                {
                    // iterate over all plugins, looking for the first one that return non-null content
                    foreach (IPackageContentViewer viewer in contentViewers)
                    {
                        using (Stream stream = file.GetStream())
                        {
                            if (size == -1)
                            {
                                size = stream.Length;
                            }
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
            if (content == null)
            {
                isBinary = FileHelper.IsBinaryFile(file.Name);
                if (isBinary)
                {
                    // don't calculate the size again if we already have it
                    if (size == -1)
                    {
                        using (Stream stream = file.GetStream())
                        {
                            size = stream.Length;
                        }
                    }
                    content = Resources.UnsupportedFormatMessage;
                }
                else
                {
                    content = ReadFileContent(file, out size);
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
            string extension = Path.GetExtension(file.Name);
            return from p in ViewModel.ContentViewerMetadata
                   where p.Metadata.SupportedExtensions.Contains(extension, StringComparer.OrdinalIgnoreCase)
                   orderby p.Metadata.Priority
                   select p.Value;
        }

        private static string ReadFileContent(PackageFile file, out long size)
        {
            const int MaxLengthToOpen = 10*1024; // limit to 10K 
            const int BufferSize = 2*1024;
            var buffer = new char[BufferSize]; // read 2K at a time

            var sb = new StringBuilder();
            Stream stream = file.GetStream();
            size = stream.Length;
            using (var reader = new StreamReader(stream))
            {
                while (sb.Length < MaxLengthToOpen)
                {
                    int bytesRead = reader.Read(buffer, 0, BufferSize);
                    if (bytesRead == 0)
                    {
                        break;
                    }
                    else
                    {
                        sb.Append(new string(buffer, 0, bytesRead));
                    }
                }

                // if not reaching the end of the stream yet, append the text "Truncating..."
                if (reader.Peek() > -1)
                {
                    // continue reading the rest of the current line to avoid dangling line
                    sb.AppendLine(reader.ReadLine());

                    if (reader.Peek() > -1)
                    {
                        sb.AppendLine().AppendLine("*** The rest of the content is truncated. ***");
                    }
                }
            }

            return sb.ToString();
        }
    }
}