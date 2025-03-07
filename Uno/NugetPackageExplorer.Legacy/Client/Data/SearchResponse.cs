#nullable disable
namespace NupkgExplorer.Client.Data
{
    public class SearchResponse
    {
        public int TotalHits { get; set; }
        public PackageData[] Data { get; set; }
    }
}
