using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection.PortableExecutable;
using System.Text;
using System.Threading.Tasks;

namespace NuGetPe.AssemblyMetadata
{
    public class AssemblyDebugData
    {
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

        public bool IsReproducible => CompilerFlags.Count > 0 && MetadataReferences.Count > 0;

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
