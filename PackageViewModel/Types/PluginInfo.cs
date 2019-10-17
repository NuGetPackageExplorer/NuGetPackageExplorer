using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using NuGet.Versioning;

namespace NuGetPackageExplorer.Types
{
    [SuppressMessage("Design", "CA1036:Override methods on comparable types", Justification = "<Pending>")]
    public class PluginInfo : IEquatable<PluginInfo>, IComparable<PluginInfo>
    {
        public PluginInfo(string id, NuGetVersion version)
        {
            if (string.IsNullOrEmpty(id))
            {
                throw new ArgumentException("Id cannot be null or empty.", nameof(id));
            }

            Id = id;
            Version = version ?? throw new ArgumentNullException(nameof(version));
        }

        public string Id { get; private set; }
        public NuGetVersion Version { get; private set; }
        
        public bool Equals(PluginInfo? other)
        {
            if (other is null) return false;

            return Id.Equals(other.Id, StringComparison.OrdinalIgnoreCase) && Version == other.Version;
        }

        public override bool Equals(object? obj)
        {
            return Equals(obj as PluginInfo);
        }

        public override string ToString()
        {
            return Id + " [" + Version + "]";
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(Id, Version);
        }

        public int CompareTo([AllowNull] PluginInfo other)
        {
            if (other is null) return -1;

            var id = StringComparer.OrdinalIgnoreCase.Compare(Id, other.Id);
            if (id != 0)
                return id;

            return Version.CompareTo(other.Version);
        }
    }
}
