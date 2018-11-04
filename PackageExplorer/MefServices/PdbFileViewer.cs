﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Controls;
using NuGetPackageExplorer.Types;
using NuGetPe.AssemblyMetadata;

namespace PackageExplorer
{
    [PackageContentViewerMetadata(100, ".pdb", SupportsWindows10S = false)]
    class PdbFileViewer : IPackageContentViewer
    {
        public object GetView(string extension, Stream stream)
        {
            using (var str = StreamUtility.MakeSeekable(stream))
            {
                var doc = AssemblyMetadataReader.ReadDebugData(str);
            }

            return new TextBlock() { Text = "PdbViewer" };
        }
    }
}