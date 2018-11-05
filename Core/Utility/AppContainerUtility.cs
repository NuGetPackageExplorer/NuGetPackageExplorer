using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using Windows.Storage;
using Microsoft.Toolkit.Win32.UI.Controls.Interop;

namespace NuGetPe
{
    public static class AppContainerUtility
    {
        
        public static bool IsInAppContainer { get; } = OSVersionHelper.IsWindows10 && HasPackageIdentity();

        // Don't load types from here accidently
        [MethodImpl(MethodImplOptions.NoInlining)]
        private static bool HasPackageIdentity()
        {
            try
            {
                var container = ApplicationData.Current.LocalSettings;
                return true;
            }
            catch
            {
            }
            return false;
        }
    }
}
