using System;

namespace NuGet {
    public class ZipPackageFactory : IPackageFactory {
        public IPackage CreatePackage(Func<System.IO.Stream> streamFactory) {
            return new ZipPackage(streamFactory);
        }
    }
}