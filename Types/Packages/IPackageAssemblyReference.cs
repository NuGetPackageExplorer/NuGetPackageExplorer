using System.Runtime.Versioning;

namespace NuGet
{
    public interface IPackageAssemblyReference : IPackageFile, IFrameworkTargetable
    {
        FrameworkName TargetFramework { get; }

        string Name { get; }
    }
}