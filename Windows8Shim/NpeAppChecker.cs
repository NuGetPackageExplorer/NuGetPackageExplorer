using System.Linq;
using System.Security.Principal;
using Windows.ApplicationModel;
using Windows.Management.Deployment;

namespace Windows8Shim
{
    public static class NpeAppChecker
    {
        public static bool CheckIsNpeMetroInstalled()
        {
            var user = WindowsIdentity.GetCurrent();
            var userSecurityId = user.Owner.Value;

            var pm = new PackageManager();


            var packages = pm.FindPackagesForUser(userSecurityId, "50582LuanNguyen.NuGetPackageExplorer_w6y2tyx5bpzwa");
            return packages.Any();
        }
    }
}