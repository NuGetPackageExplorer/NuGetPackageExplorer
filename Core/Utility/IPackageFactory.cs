using System;
using System.IO;

namespace NuGet {
    public interface IPackageFactory {
        IPackage CreatePackage(Func<Stream> streamFactory);
    }
}
