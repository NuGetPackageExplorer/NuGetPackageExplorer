using System;
using System.Collections.Generic;
using System.Data.Services.Client;
using System.Diagnostics.CodeAnalysis;
using System.Linq;

namespace NuGet
{
    public class DataServicePackageRepository : IPackageRepository
    {
        private readonly DataServiceContext _context;
        private readonly IHttpClient _httpClient;
        private DataServiceQuery<DataServicePackage> _query;
        private readonly Lazy<ISet<string>> _supportedProperties;

        public DataServicePackageRepository(IHttpClient httpClient)
        {
            if (httpClient == null)
            {
                throw new ArgumentNullException("httpClient");
            }
            _httpClient = httpClient;
            _httpClient.AcceptCompression = true;

            _context = new DataServiceContext(httpClient.Uri);
            _context.SendingRequest += OnSendingRequest;
            _context.IgnoreMissingProperties = true;
            _supportedProperties = new Lazy<ISet<string>>(() => _context.GetDataServiceMetadata());
        }

        #region IPackageRepository Members

        public string Source
        {
            get { return _context.BaseUri.OriginalString; }
        }

        IQueryable<IPackage> IPackageRepository.GetPackages()
        {
            return GetPackages();
        }

        public bool SupportsPrereleasePackages
        {
            get
            {
                return _supportedProperties.Value != null && _supportedProperties.Value.Contains("IsAbsoluteLatestVersion");
            }
        }

        #endregion

        private void OnSendingRequest(object sender, SendingRequestEventArgs e)
        {
            _httpClient.InitializeRequest(e.Request);
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
    }
}