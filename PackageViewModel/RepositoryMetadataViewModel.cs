using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using NuGet.Packaging.Core;

namespace PackageExplorerViewModel
{
    /// <summary>
    /// Workaround for https://github.com/dotnet/wpf/issues/3050
    /// </summary>
    public class RepositoryMetadataViewModel
    {
        public string Type
        {
            get;
            set;
        } = string.Empty;


        public string Url
        {
            get;
            set;
        } = string.Empty;


        public string Branch
        {
            get;
            set;
        } = string.Empty;


        public string Commit
        {
            get;
            set;
        } = string.Empty;


        public RepositoryMetadataViewModel()
        {
        }

        public RepositoryMetadataViewModel(string type, string url, string branch, string commit)
        {
            Type = type;
            Url = url;
            Branch = branch;
            Commit = commit;
        }

        public RepositoryMetadataViewModel(RepositoryMetadata metadata)
        {
            if (metadata is null)
                throw new ArgumentNullException(nameof(metadata));

            Type = metadata.Type;
            Url = metadata.Url;
            Branch = metadata.Branch;
            Commit = metadata.Commit;
        }
    }
}
