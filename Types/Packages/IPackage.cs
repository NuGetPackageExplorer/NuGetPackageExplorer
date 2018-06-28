using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using NuGet.Packaging;

namespace NuGetPe
{
    public interface IPackage : IPackageMetadata, IServerPackageMetadata, IDisposable
    {
        bool IsAbsoluteLatestVersion { get; }

        bool IsLatestVersion { get; }

        bool IsPrerelease { get; }

        DateTimeOffset LastUpdated { get; }

        DateTimeOffset? Published { get; }

        [SuppressMessage("Microsoft.Design", "CA1024:UsePropertiesWhereAppropriate",
            Justification = "This might be expensive")]
        IEnumerable<IPackageFile> GetFiles();

        [SuppressMessage("Microsoft.Design", "CA1024:UsePropertiesWhereAppropriate",
            Justification = "This might be expensive")]
        Stream GetStream();
    }
}
