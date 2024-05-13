using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using NuGetPackageExplorer.Types;
using NuGetPe;
using NuGetPe.AssemblyMetadata;
using PackageExplorerViewModel;

#if HAS_UNO || USE_WINUI
using Microsoft.UI.Xaml.Controls;
using Uno.Extensions;
using Uno.Logging;
#else
using System.Windows.Controls;
#endif

namespace PackageExplorer
{
    [PackageContentViewerMetadata(100, ".pdb", SupportsWindows10S = false)]
    internal class PdbFileViewer : IPackageContentViewer
    {
        public object GetView(IPackageContent selectedFile, IReadOnlyList<IPackageContent> peerFiles)
        {
            DiagnosticsClient.TrackEvent("PdbFileViewer");

            IPackageContent? pe = null;

            if (selectedFile.Name.EndsWith(".ni.pdb", StringComparison.OrdinalIgnoreCase))
            {
                // This case is to ensure we are prioritize the ni dll (ngen framework case).
                pe = peerFiles.FirstOrDefault(pc => pc.Name.EndsWith(".ni.dll", StringComparison.OrdinalIgnoreCase));
            }

            // Cascade to trying the dll/winmd, this is the crossgen/crossgen2 case of .NET Core/.NET 5+
            pe ??= peerFiles.FirstOrDefault(pc => ".dll".Equals(Path.GetExtension(pc.Name), StringComparison.OrdinalIgnoreCase) ||
                                                ".winmd".Equals(Path.GetExtension(pc.Name), StringComparison.OrdinalIgnoreCase));

            // Get the exe as a last resort, the stand-alone case (.NET Single file or .NET Framework app)
            pe ??= peerFiles.FirstOrDefault(pc =>  ".exe".Equals(Path.GetExtension(pc.Name), StringComparison.OrdinalIgnoreCase));

#pragma warning disable CA2000 // Dispose objects before losing scope -- ReadDebugData will dispose
            var peStream = pe != null
                ? StreamUtility.MakeSeekable(pe.GetStream(), true)
                : null;

            // This might throw an exception because we don't know if it's a full PDB or portable
            // Try anyway in case it succeeds as a ppdb
            try
            {
                var stream = StreamUtility.MakeSeekable(selectedFile.GetStream(), true);
                var data = new AssemblyDebugDataViewModel(AssemblyMetadataReader.ReadDebugData(peStream, stream));

#if !HAS_UNO && !USE_WINUI
                // Tab control with two pages
                var tc = new TabControl()
                {
                    Items =
                        {
                            new TabItem
                            {
                                Header = "PDB Info",
                                Content = new ScrollViewer
                                {
                                    HorizontalScrollBarVisibility = ScrollBarVisibility.Auto,
                                    VerticalScrollBarVisibility = ScrollBarVisibility.Auto,
                                    Content = new Controls.PdbInfoViewer
                                    {
                                        DataContext = data
                                    }
                                }
                            },
                            new TabItem
                            {
                                Header = "PDB Sources",
                                Content = new ScrollViewer
                                {
                                    HorizontalScrollBarVisibility = ScrollBarVisibility.Auto,
                                    VerticalScrollBarVisibility = ScrollBarVisibility.Auto,
                                    Content = new Controls.PdbSourcesViewer
                                    {
                                        DataContext = data
                                    }
                                }
                            }
                        }
                };

                return tc;
#else
                // returning UIElement from here works.
                // however due to performance issues, we are just
                // returning the datacontext and letting the xaml to handle the view.
                // also, the ui layout is vastly different compared to the #if-block above
                return new AssemblyFileViewer.AssemblyFileContent()
                {
                    Metadata = null,
                    DebugData = data,
                };
#endif
            }
            catch (ArgumentNullException)
            {

            }
            catch (Exception e)
            {
#if HAS_UNO || USE_WINUI
                this.Log().Error("Failed to generate view", e);
#endif
            }

#pragma warning restore CA2000 // Dispose objects before losing scope
            return new TextBlock()
            {
                Text = "Full PDB files requires the EXE or DLL to be alongside."
            };
        }
    }
}
