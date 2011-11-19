using System;
using System.IO;

namespace NuGet
{
    public interface IGalleryServer
    {
        bool IsV1Protocol { get; }
        string Source { get; }
        void PushPackage(string apiKey, Stream packageStream, IObserver<int> progressObserver, IPackageMetadata package);
    }
}
