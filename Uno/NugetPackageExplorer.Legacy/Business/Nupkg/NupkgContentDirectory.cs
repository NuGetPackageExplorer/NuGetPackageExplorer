namespace NupkgExplorer.Business.Nupkg
{
    public class NupkgContentDirectory : INupkgFileSystemObject
    {
        public string Name { get; }

        public string FullName { get; }

        public List<INupkgFileSystemObject> Items { get; }

        public NupkgContentDirectory(string fullname, IEnumerable<INupkgFileSystemObject>? items = null)
        {
            Name = Path.GetFileName(fullname);
            FullName = fullname;
            Items = items?.ToList() ?? [];
        }
    }
}
