using System;
using System.Data.Services.Client;
using System.Linq;

namespace NuGet {
    public class DataServicePackageRepository : IPackageRepository {
        private readonly DataServiceContext _context;
        private DataServiceQuery<DataServicePackage> _query;
        private readonly IHttpClient _client;

        public DataServicePackageRepository(IHttpClient client)
        {
            if (null == client)
            {
                throw new ArgumentNullException("client");
            }
            _client = client;
            _context = new DataServiceContext(client.Uri);

            _context.SendingRequest += OnSendingRequest;
            _context.IgnoreMissingProperties = true;
        }

        public string Source {
            get {
                return _context.BaseUri.OriginalString;
            }
        }

        private void OnSendingRequest(object sender, SendingRequestEventArgs e) {
            _client.InitializeRequest(e.Request);
        }

        IQueryable<IPackage> IPackageRepository.GetPackages() {
            return GetPackages();
        }

        public Uri GetReadStreamUri(object entity) {
            return _context.GetReadStreamUri(entity);
        }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Design", "CA1024:UsePropertiesWhereAppropriate")]
        public IQueryable<DataServicePackage> GetPackages() {
            if (_query == null) {
                _query = _context.CreateQuery<DataServicePackage>(Constants.PackageServiceEntitySetName);
            }
            return _query;
        }
    }
}