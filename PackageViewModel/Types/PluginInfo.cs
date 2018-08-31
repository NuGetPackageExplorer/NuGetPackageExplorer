using System;
using NuGet.Versioning;

namespace NuGetPackageExplorer.Types
{
    public class PluginInfo : IEquatable<PluginInfo>
    {
        public PluginInfo(string id, NuGetVersion version)
        {
            if (string.IsNullOrEmpty(id))
            {
                throw new ArgumentException("Id cannot be null or empty.", "id");
            }

            Id = id;
            Version = version ?? throw new ArgumentNullException("version");
        }

        public string Id { get; private set; }
        public NuGetVersion Version { get; private set; }

        #region IEquatable<PluginInfo> Members

        public bool Equals(PluginInfo other)
        {
            return Id.Equals(other.Id, StringComparison.OrdinalIgnoreCase) && Version == other.Version;
        }

        #endregion

        public override string ToString()
        {
            return Id + " [" + Version + "]";
        }

        public override int GetHashCode()
        {
            return Id.GetHashCode() * 3137 + Version.GetHashCode();
        }
    }
}