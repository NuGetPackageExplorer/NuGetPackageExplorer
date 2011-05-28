using System;
using System.Net;

namespace NuGet {
    public interface IProxyService {
        IWebProxy GetProxy(Uri uri);
    }
}
