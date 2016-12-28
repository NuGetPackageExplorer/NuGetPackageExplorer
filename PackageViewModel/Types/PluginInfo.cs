using System;
using NuGetPe;

namespace NuGetPackageExplorer.Types
{
    public class PluginInfo : IEquatable<PluginInfo>
    {
        public PluginInfo(string id, TemplatebleSemanticVersion version)
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

        public string Id { get; private set; }
        public TemplatebleSemanticVersion Version { get; private set; }

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
            return Id.GetHashCode()*3137 + Version.GetHashCode();
        }
    }
}