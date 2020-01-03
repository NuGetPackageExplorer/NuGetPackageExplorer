using NuGet.Packaging;

namespace PackageExplorerViewModel
{
    public interface IEditablePackageFile : IPackageFile
    {
        string Name { get; }
        bool Save(string editedFilePath);

        string? OriginalPath { get; }
    }
}
