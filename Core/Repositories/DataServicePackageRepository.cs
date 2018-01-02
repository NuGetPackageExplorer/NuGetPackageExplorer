using System;
using System.Data.Services.Client;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Net;

namespace NuGetPe
{
    public class DataServicePackageRepository : IPackageRepository, IPackageSearchable
    {
        private readonly DataServiceContext _context;
        private DataServiceQuery<DataServicePackage> _query;

	    public DataServicePackageRepository(Uri uri, ICredentials credentials)
	    {
            _context = new DataServiceContext(uri)
            {
                Credentials = credentials
            };
            _context.SendingRequest += OnSendingRequest;
		    _context.IgnoreMissingProperties = true;
		    _context.Credentials = credentials;
	    }

	    public string Source
        {
            get { return _context.BaseUri.OriginalString; }
        }

        IQueryable<IPackage> IPackageRepository.GetPackages()
        {
            return GetPackages();
        }

        IQueryable<IPackage> IPackageRepository.GetPackagesById(string id, bool includePrerelease)
        {
            return GetPackagesById(id, includePrerelease);
        }

        private void OnSendingRequest(object sender, SendingRequestEventArgs e)
        {
            var httpRequest = e.Request as HttpWebRequest;
            httpRequest.Proxy = HttpWebRequest.DefaultWebProxy;
            httpRequest.Proxy.Credentials = CredentialCache.DefaultNetworkCredentials;

            if (httpRequest != null)
            {
                httpRequest.UserAgent = HttpUtility.CreateUserAgentString("NuGet Package Explorer");
                httpRequest.AutomaticDecompression = DecompressionMethods.Deflate | DecompressionMethods.GZip;
            }
        }

        public Uri GetReadStreamUri(object entity)
        {
            return _context.GetReadStreamUri(entity);
        }

        [SuppressMessage("Microsoft.Design", "CA1024:UsePropertiesWhereAppropriate")]
        public IQueryable<DataServicePackage> GetPackages()
        {
            if (_query == null)
            {
                _query = _context.CreateQuery<DataServicePackage>(Constants.PackageServiceEntitySetName).IncludeTotalCount();
            }
            return _query;
        }

        public IQueryable<DataServicePackage> GetPackagesById(string id, bool includePrerelease)
        {
            IQueryable<DataServicePackage> query =
                _context.CreateQuery<DataServicePackage>("FindPackagesById")
                        .AddQueryOption("id", "'" + id + "'");

            if (!includePrerelease)
            {
                query = query.Where(p => !p.IsPrerelease);
            }

            return query;
        }

        public IQueryable<DataServicePackage> LegacyGetPackagesById(string id)
        {
            var query = _context.CreateQuery<DataServicePackage>(Constants.PackageServiceEntitySetName)
                                                           .Where(p => p.Id.ToLower() == id.ToLower());

            return query;
        }

        public IQueryable<IPackage> Search(string searchTerm, bool includePrerelease)
        {
            if (searchTerm.StartsWith("id:", StringComparison.OrdinalIgnoreCase))
            {
                var id = searchTerm.Substring(3).Trim();
                if (string.IsNullOrEmpty(id))
                {
                    return new IPackage[0].AsQueryable();
                }

                return _context.CreateQuery<DataServicePackage>("FindPackagesById")
                                .AddQueryOption("id", "'" + id + "'")
                                .IncludeTotalCount();
            }
            else
            {
                return _context.CreateQuery<DataServicePackage>("Search")
                                .AddQueryOption("searchTerm", "'" + searchTerm + "'")
                                .AddQueryOption("targetFramework", "")
                                .AddQueryOption("includePrerelease", includePrerelease ? "true" : "false")
                                .IncludeTotalCount();
            }
        }
    }
}
