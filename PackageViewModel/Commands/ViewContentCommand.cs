using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Linq;
using System.Text;
using System.Windows.Input;
using AuthenticodeExaminer;
using NuGetPackageExplorer.Types;
using NuGetPe;
using PackageExplorerViewModel.Utilities;

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
            DiagnosticsClient.TrackEvent("ViewContentCommand");

            if ("Hide".Equals(parameter))
            {
                ViewModel.ShowContentViewer = false;
            }
            else
            {
                if ((parameter ?? ViewModel.SelectedItem) is PackageFile file)
                {
                    try
                    {
                        ShowFile(file);
                    }
                    catch (Exception e)
                    {
                        if (!(e is IOException))
                        {
                            DiagnosticsClient.TrackException(e);
                        }

                        ViewModel.UIServices.Show(e.Message, MessageLevel.Error);
                    }

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
            object? content = null;
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

                        // Get peer files
                        var peerFiles = file.Parent!.GetFiles()
                                            .Select(pf => new PackageFile(pf, Path.GetFileName(pf.Path), file.Parent!))
                                            .ToList();

                        content = viewer.GetView(file, peerFiles);
                        if (content != null)
                        {
                            // found a plugin that can read this file, stop
                            break;
                        }
                    }
                }
                catch (Exception ex) when (!(ex is FileNotFoundException))
                {
                    DiagnosticsClient.TrackException(ex);
                    // don't let plugin crash the app
                    content = Resources.PluginFailToReadContent + Environment.NewLine + ex.ToString();
                }

                if (content is string)
                {
                    isBinary = false;
                }
            }

            // if plugins fail to read this file, fall back to the default viewer
            var truncated = false;
            if (content == null)
            {
                isBinary = FileHelper.IsBinaryFile(file.Name);
                if (isBinary)
                {
                    content = Resources.UnsupportedFormatMessage;
                }
                else
                {
                    content = ReadFileContent(file, out truncated);
                }
            }

            long size = -1;
            IReadOnlyList<AuthenticodeSignature> sigs;
            SignatureCheckResult isValidSig;
            using (var str = file.GetStream())
            using (var tempFile = new TemporaryFile(str, Path.GetExtension(file.Name)))
            {
                var extractor = new FileInspector(tempFile.FileName);

                sigs = extractor.GetSignatures().ToList();
                isValidSig = extractor.Validate();

                size = tempFile.Length;
            }

            var fileInfo = new FileContentInfo(
                file,
                file.Path,
                content,
                !isBinary,
                size,
                truncated,
                sigs,
                isValidSig);

            ViewModel.ShowFile(fileInfo);
        }

        private IEnumerable<IPackageContentViewer> FindContentViewer(PackageFile file)
        {
            var extension = Path.GetExtension(file.Name);

            return from p in ViewModel.ContentViewerMetadata
                   where AppCompat.IsWindows10S ? p.Metadata.SupportsWindows10S : true // Filter out incompatible addins on 10s
                   where p.Metadata.SupportedExtensions.Contains(extension, StringComparer.OrdinalIgnoreCase)
                   orderby p.Metadata.Priority
                   select p.Value;
        }


        private static string ReadFileContent(PackageFile file, out bool truncated)
        {
            var buffer = new char[1024 * 32];
            truncated = false;
            using var stream = file.GetStream();
            using var reader = new StreamReader(stream);
            // Read 500 kb
            const int maxBytes = 500 * 1024;
            var sb = new StringBuilder();

            int bytesRead;

            while ((bytesRead = reader.Read(buffer, 0, buffer.Length)) > 0)
            {
                sb.Append(buffer, 0, bytesRead);
                if (sb.Length >= maxBytes)
                {
                    truncated = true;
                    break;
                }
            }

            return sb.ToString();
        }
    }
}
