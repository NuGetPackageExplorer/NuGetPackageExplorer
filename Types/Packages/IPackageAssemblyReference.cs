using System.Runtime.Versioning;

namespace NuGetPe
{
    public interface IPackageAssemblyReference : IPackageFile
    {
        string Name { get; }
    }
}