using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;

namespace PackageExplorer
{
    // From https://github.com/vcsjones/FiddlerCert/blob/06642751314a9ff224cb37a1cd7c14b86062a119/VCSJones.FiddlerCert/DistinguishedNameParser.cs
    public static class DistinguishedNameParser
    {
        public static Dictionary<string, List<string>> Parse(string distingishedName)
        {
            var result = new Dictionary<string, List<string>>(StringComparer.CurrentCultureIgnoreCase);
            var distinguishedNamePtr = IntPtr.Zero;
            try
            {
                distinguishedNamePtr = Marshal.StringToCoTaskMemUni(distingishedName);
                //We need to copy the IntPtr.
                //The copy is necessary because DsGetRdnW modifies the pointer to advance it. We need to keep
                //The original so we can free it later, otherwise we'll leak memory.
                var distinguishedNamePtrCopy = distinguishedNamePtr;
                var pcDN = (uint)distingishedName.Length;
                while (pcDN != 0 && NativeMethods.DsGetRdnW(ref distinguishedNamePtrCopy, ref pcDN, out var ppKey, out var pcKey, out var ppVal, out var pcVal) == 0)
                {
                    if (pcKey == 0 || pcVal == 0)
                    {
                        continue;
                    }
                    var key = Marshal.PtrToStringUni(ppKey, (int)pcKey);
                    var value = Marshal.PtrToStringUni(ppVal, (int)pcVal);
                    if (result.ContainsKey(key))
                    {
                        result[key].Add(value);
                    }
                    else
                    {
                        result.Add(key, new List<string> { value });
                    }
                    if (pcDN == 0)
                    {
                        break;
                    }
                }
                return result;

            }
            finally
            {
                Marshal.FreeCoTaskMem(distinguishedNamePtr);
            }
        }
    }
}
