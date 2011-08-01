using System.Diagnostics.CodeAnalysis;
using System.IO;

namespace NuGet {    
    public interface IPackageFile {
        string Path {
            get;
        }

        [SuppressMessage("Microsoft.Design", "CA1024:UsePropertiesWhereAppropriate", Justification = "This might be expensive")]
        Stream GetStream();       
    }
}
