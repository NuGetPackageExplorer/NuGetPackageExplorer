using System;

namespace NuGetPackageExplorer.Types
{
    public enum PackageType {
        LocalPackage,
        DataServicePackage
    }

    public sealed class MruItem : IEquatable<MruItem>
    {
        public string Id { get; set; }
        public Version Version { get; set; }
        public string Path { get; set; }
        public PackageType PackageType { get; set; }

        public bool Equals(MruItem other)
        {
            if (other == null)
            {
                return false;
            }
            return Path.Equals(other.Path, StringComparison.OrdinalIgnoreCase);
        }

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