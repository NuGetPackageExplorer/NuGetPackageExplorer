using System.Security.Principal;
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
            var package = pm.FindPackageForUser(userSecurityId, "9238fdee-d032-4145-aac5-f55d90c440d9_1.0.0.0_neutral__ynymmkm2tp3bw");
            return package != null;
        }
    }
}