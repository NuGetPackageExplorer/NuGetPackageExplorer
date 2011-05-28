using System.IO;
using System.Collections.Generic;
using System;

namespace NuGetPackageExplorer.Types {
    public interface IPluginManager {
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Design", "CA1021:AvoidOutParameters", MessageId = "1#")]
        bool AddPluginFromAssembly(string assemblyPath, out FileInfo file);
        bool DeletePlugin(FileInfo file);
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Design", "CA1024:UsePropertiesWhereAppropriate")]
        IEnumerable<FileInfo> GetAllPlugins();
    }
}