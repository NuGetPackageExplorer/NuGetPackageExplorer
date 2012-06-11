using NuGet;

namespace PackageExplorerViewModel
{
    public interface IEditablePackageFile : IPackageFile
    {
        bool AskToSaveOnClose { get; }
        string OriginalPath { get; }
        string Name { get; }
        bool Save(string editedFilePath);
    }
}