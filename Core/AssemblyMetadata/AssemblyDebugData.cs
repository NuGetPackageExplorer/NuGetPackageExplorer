using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection.PortableExecutable;
using System.Text;
using System.Threading.Tasks;

using NuGet.Protocol.Plugins;

namespace NuGetPe.AssemblyMetadata
{
    public class AssemblyDebugData
    {
        private const int MIN_COMPILER_METADATA_VERSION_FOR_REPRODUCIBLE_BUILDS = 2;
        public AssemblyDebugData()
        {
            SourceLinkErrors = new List<string>();
            Sources = new List<AssemblyDebugSourceDocument>();
            SymbolKeys = new List<SymbolKey>();
            MetadataReferences = new List<MetadataReference>();
            CompilerFlags = new List<CompilerFlag>();

            _untrackedSources = new Lazy<IReadOnlyList<string>>(() => GetNonEmbeddedSourcesInObjDir());
            _sourcesAreDeterministic = new Lazy<bool>(CalculateSourcesDeterministic);
        }

        private readonly Lazy<IReadOnlyList<string>> _untrackedSources;
        private readonly Lazy<bool> _sourcesAreDeterministic;

        public PdbType PdbType { get; internal set; }

        public IReadOnlyList<AssemblyDebugSourceDocument> Sources { get; internal set; }
        public IReadOnlyList<string> SourceLinkErrors { get; internal set; }
        public IReadOnlyList<SymbolKey> SymbolKeys { get; internal set; }

        public IReadOnlyCollection<MetadataReference> MetadataReferences { get; internal set;}
        public IReadOnlyCollection<CompilerFlag> CompilerFlags { get; internal set;}

        public bool PdbChecksumIsValid { get; internal set; }

        public bool HasSourceLink => Sources.Any(doc => doc.HasSourceLink);

        public bool AllSourceLink => Sources.All(doc => doc.HasSourceLink);

        /// <summary>
        /// True if we hae PDB data loaded
        /// </summary>
        public bool HasDebugInfo { get; internal set; }

        public bool HasCompilerFlags => CompilerFlags.Count > 0 && MetadataReferences.Count > 0;

        public IReadOnlyList<string> UntrackedSources => _untrackedSources.Value;

        public bool SourcesAreDeterministic => _sourcesAreDeterministic.Value;

        private IReadOnlyList<string> GetNonEmbeddedSourcesInObjDir()
        {
            // get sources where /obj/ is in the name and it's not
            // Document names may use either / or \ a directory separator

            var docs = (from doc in Sources
                        let path = doc.Name.Replace('\\', '/')
                        where (path.Contains("/obj/", StringComparison.OrdinalIgnoreCase) ||
                               path.Contains("/temp/", StringComparison.OrdinalIgnoreCase) ||
                               path.Contains("/tmp/", StringComparison.OrdinalIgnoreCase) ) && !doc.IsEmbedded
                        select doc.Name).ToList();

            return docs;
        }

        public bool CompilerVersionSupportsReproducible
        {
            get
            {
                if (!HasCompilerFlags)
                    return false;


                // See if it has the new version compiler flat, added in 3.9.0 that indicates
                // we have the min compiler version we can support for this


                var versionString = CompilerFlags.Where(f => f.Key == "version")
                                           .Select(f => f.Value)
                                           .FirstOrDefault();

                // if missing, the compiler is too old
                if (versionString == null)
                    return false;

                if(!int.TryParse(versionString, out var version))
                {
                    return false; // could not parse version
                }

                return version >= MIN_COMPILER_METADATA_VERSION_FOR_REPRODUCIBLE_BUILDS;
            }
        }

        private bool CalculateSourcesDeterministic()
        {
            return Sources.All(doc => doc.Name.StartsWith("/_", StringComparison.OrdinalIgnoreCase));
        }

    }

    public enum PdbType
    {
        Portable,
        Embedded,
        Full
    }
}
