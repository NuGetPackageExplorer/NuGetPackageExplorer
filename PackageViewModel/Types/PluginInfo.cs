using System;
using NuGet;

namespace NuGetPackageExplorer.Types
{
    public class PluginInfo : IEquatable<PluginInfo>
    {
        public string Id { get; private set; }
        public SemanticVersion Version { get; private set; }

        public PluginInfo(string id, SemanticVersion version)
        {
            if (String.IsNullOrEmpty(id))
            {
                throw new ArgumentException("Id cannot be null or empty.", "id");
            }
            if (version == null)
            {
                throw new ArgumentNullException("version");
            }

            Id = id;
            Version = version;
        }

        public override string ToString()
        {
            return Id + " [" + Version.ToString() + "]";
        }

        public bool Equals(PluginInfo other)
        {
            return Id.Equals(other.Id, StringComparison.OrdinalIgnoreCase) && Version == other.Version;
        }

        public override int GetHashCode()
        {
            return Id.GetHashCode() * 3137 + Version.GetHashCode();
        }
    }
}
