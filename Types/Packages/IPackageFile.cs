using NuGet.Frameworks;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Runtime.Versioning;

namespace NuGetPe
{
    public interface IPackageFile : IFrameworkTargetable
    {
        string Path { get; }

        string OriginalPath { get; }

        [SuppressMessage("Microsoft.Design", "CA1024:UsePropertiesWhereAppropriate", Justification = "This might be expensive")]
        Stream GetStream();

        /// <summary>
        /// Gets the path that excludes the root folder (content/lib/tools) and framework folder (if present).
        /// </summary>
        /// <example>
        /// If a package has the Path as 'content\[net40]\scripts\jQuery.js', the EffectivePath 
        /// will be 'scripts\jQuery.js'.
        /// 
        /// If it is 'tools\init.ps1', the EffectivePath will be 'init.ps1'.
        /// </example>
        string EffectivePath
        {
            get;
        }

        FrameworkName TargetFramework
        {
            get;
        }
    }
}