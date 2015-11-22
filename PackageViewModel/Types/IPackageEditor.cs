namespace NuGetPackageExplorer.Types
{
    public interface IPackageEditorService
    {
        void BeginEdit();
        void CancelEdit();
        bool CommitEdit();
    }
}