using System.ComponentModel.Composition;
using NuGet;

namespace PackageExplorer {
    /// <summary>
    /// This class is just a bootstrapper that allows us to force MEF
    /// to use the WindowsCredentialsProvider otherwise you can try using ProxyService
    /// in the Core assembly.
    /// </summary>
    [Export(typeof(IProxyService))]
    [PartCreationPolicy(CreationPolicy.Shared)]
    public class WindowsProxyService : ProxyService {
        public WindowsProxyService() : base(new WindowsCredentialProvider()) {
        }
    }
}
