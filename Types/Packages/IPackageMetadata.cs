using System;
using System.Collections.Generic;

namespace NuGet
{
    public interface IPackageMetadata
    {
        string Id { get; }
        SemanticVersion Version { get; }
        string Title { get; }
        IEnumerable<string> Authors { get; }
        IEnumerable<string> Owners { get; }
        Uri IconUrl { get; }
        Uri LicenseUrl { get; }
        Uri ProjectUrl { get; }
        bool RequireLicenseAcceptance { get; }
        bool DevelopmentDependency { get; }
        string Description { get; }
        string Summary { get; }
        string ReleaseNotes { get; }
        string Copyright { get; }
        string Language { get; }
        string Tags { get; }
        bool Serviceable { get; }
        IEnumerable<FrameworkAssemblyReference> FrameworkAssemblies { get; }

        /// <summary>
        /// Specifies sets other packages that the package depends on.
        /// </summary>
        IEnumerable<PackageDependencySet> DependencySets { get; }

        /// <summary>
        /// Returns sets of References specified in the manifest.
        /// </summary>
        IEnumerable<PackageReferenceSet> PackageAssemblyReferences { get; }

        Version MinClientVersion { get; }
    }
}