using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Windows.Controls;
using NuGetPackageExplorer.Types;
using NuGetPe;
using NuGetPe.AssemblyMetadata;
using PackageExplorerViewModel;

namespace PackageExplorer
{
    [PackageContentViewerMetadata(100, ".pdb", SupportsWindows10S = false)]
    internal class PdbFileViewer : IPackageContentViewer
    {
        public object GetView(IPackageContent selectedFile, IReadOnlyList<IPackageContent> peerFiles)
        {
            DiagnosticsClient.TrackEvent("PdbFileViewer");

            AssemblyDebugDataViewModel? data = null;

            // Get the PE file, exe or dll that matches
            var filename = Path.GetFileNameWithoutExtension(selectedFile.Name);
            var pe = peerFiles.FirstOrDefault(pc => pc.Path != selectedFile.Path &&
                                                    Path.GetFileNameWithoutExtension(pc.Name).Equals(filename, StringComparison.OrdinalIgnoreCase) &&
                                                    (".dll".Equals(Path.GetExtension(pc.Name), StringComparison.OrdinalIgnoreCase) ||
                                                     ".exe".Equals(Path.GetExtension(pc.Name), StringComparison.OrdinalIgnoreCase)));

            Stream? peStream = null;
            try
            {
                if (pe != null) // we have a matching file
                {
                    peStream = StreamUtility.MakeSeekable(pe.GetStream(), true);
                }


                // This might throw an exception because we don't know if it's a full PDB or portable
                // Try anyway in case it succeeds as a ppdb
                try
                {
                    using (var stream = StreamUtility.MakeSeekable(selectedFile.GetStream(), true))
                    {
                        data = new AssemblyDebugDataViewModel(AssemblyMetadataReader.ReadDebugData(peStream, stream));
                    }

                    return new ScrollViewer
                    {
                        HorizontalScrollBarVisibility = ScrollBarVisibility.Auto,
                        VerticalScrollBarVisibility = ScrollBarVisibility.Auto,
                        Content = new Controls.PdbFileViewer
                        {
                            DataContext = data
                        }
                    };
                }
                catch (ArgumentNullException)
                {

                }
            }
            finally
            {
                peStream?.Dispose();
            }

            return new TextBlock()
            {
                Text = "Full PDB files rquired the EXE or DLL to be alongside."
            };

        }
    }
}
