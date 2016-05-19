using NuGetPe;

namespace PackageExplorerViewModel
{
    public interface IEditablePackageFile : IPackageFile
    {
        string Name { get; }
        bool Save(string editedFilePath);
    }
}