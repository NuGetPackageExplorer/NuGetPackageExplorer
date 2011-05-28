using System.IO;

namespace NuGet {
    internal class PathSearchFilter {
        public string SearchDirectory { get; set; }

        public SearchOption SearchOption { get; set; }

        public string SearchPattern { get; set; }

        public bool WildCardSearch { get; set; }
    }
}
