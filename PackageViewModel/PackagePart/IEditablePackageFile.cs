using NuGet;

namespace PackageExplorerViewModel
{
    public interface IEditablePackageFile : IPackageFile
    {
        string OriginalPath { get; }
        string Name { get; }
        bool Save(string editedFilePath);
    }
}