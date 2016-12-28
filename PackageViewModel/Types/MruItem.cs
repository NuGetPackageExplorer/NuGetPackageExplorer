using System;
using NuGetPe;

namespace NuGetPackageExplorer.Types
{
    public enum PackageType
    {
        LocalPackage,
        DataServicePackage
    }

    public sealed class MruItem : IEquatable<MruItem>
    {
        public string Id { get; set; }
        public TemplatebleSemanticVersion Version { get; set; }
        public string Path { get; set; }
        public PackageType PackageType { get; set; }

        #region IEquatable<MruItem> Members

        public bool Equals(MruItem other)
        {
            if (other == null)
            {
                return false;
            }
            return Path.Equals(other.Path, StringComparison.OrdinalIgnoreCase);
        }

        #endregion

        public override bool Equals(object obj)
        {
            return Equals(obj as MruItem);
        }

        public override int GetHashCode()
        {
            return Path.GetHashCode();
        }
    }
}