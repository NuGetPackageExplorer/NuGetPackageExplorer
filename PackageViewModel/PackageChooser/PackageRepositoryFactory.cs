using System;
using NuGet;

namespace PackageExplorerViewModel
{
    internal static class PackageRepositoryFactory
    {
        public static IPackageRepository CreateRepository(string source)
        {
            if (source == null)
            {
                throw new ArgumentNullException("source");
            }

            var uri = new Uri(source);

            if (uri.IsFile)
            {
                return new LocalPackageRepository(source);
            }
            else
            {
                return new DataServicePackageRepository(uri);
            }
        }
    }
}