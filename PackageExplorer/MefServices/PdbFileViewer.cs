using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using NuGetPackageExplorer.Types;
using NuGetPe.AssemblyMetadata;
using PackageExplorerViewModel;

namespace PackageExplorer
{
    [PackageContentViewerMetadata(100, ".pdb", SupportsWindows10S = false)]
    class PdbFileViewer : IPackageContentViewer
    {
        public object GetView(string extension, Stream stream)
        {
            AssemblyDebugDataViewModel data = null;

            try
            {
                using (var str = StreamUtility.MakeSeekable(stream))
                {
                    data = new AssemblyDebugDataViewModel(AssemblyMetadataReader.ReadDebugData(str));
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
            catch 
            {
                return new TextBlock()
                {
                    Text = "Full PDB's are not supported yet. Portable and Embedded are currently supported.",
                    Margin = new Thickness(3)
                };
            }
            
        }
    }
}
