using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Linq;
using System.Reflection.Metadata;
using System.Text;
using System.Threading.Tasks;

namespace NuGetPe.AssemblyMetadata
{
    [DebuggerDisplay("Name = {Name}")]
    public struct MetadataReference : IEquatable<MetadataReference>
    {
        public string Name { get; set; }
        public ImmutableArray<string> ExternAliases { get; set; }
        public MetadataImageKind MetadataImageKind { get; set; }
        public bool EmbedInteropTypes { get; set; }
        public int Timestamp { get; set; }
        public int ImageSize { get; set; }
        public Guid Mvid { get; set; }

        public override bool Equals(object? obj)
        {
            return obj is MetadataReference reference && Equals(reference);
        }

        public bool Equals(MetadataReference other)
        {
            return Name == other.Name &&
                   ExternAliases == other.ExternAliases &&
                   MetadataImageKind == other.MetadataImageKind &&
                   EmbedInteropTypes == other.EmbedInteropTypes &&
                   Timestamp == other.Timestamp &&
                   ImageSize == other.ImageSize &&
                   Mvid.Equals(other.Mvid);
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(Name, ExternAliases, MetadataImageKind, EmbedInteropTypes, Timestamp, ImageSize, Mvid);
        }

        public static bool operator ==(MetadataReference left, MetadataReference right)
        {
            return left.Equals(right);
        }

        public static bool operator !=(MetadataReference left, MetadataReference right)
        {
            return !(left == right);
        }
    }

    public enum MetadataImageKind
    {
        Assembly,
        Module
    }
}
