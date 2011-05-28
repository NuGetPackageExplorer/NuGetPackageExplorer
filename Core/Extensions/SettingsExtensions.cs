using System;
using System.Security.Cryptography;
using System.Text;
//using Microsoft.Internal.Web.Utils;

namespace NuGet {
    public static class SettingsExtensions {
        private static string _entropy = "NuGet";

        public static string GetDecryptedValue(this ISettings settings, string section, string key) {
            if (String.IsNullOrEmpty(section)) {
                throw new ArgumentException("Argument cannot be null or empty.", "section");
            }

            if (String.IsNullOrEmpty(key)) {
                throw new ArgumentException("Argument cannot be null or empty.", "key");
            }

            var encrpytedString = settings.GetValue(section, key);
            if (encrpytedString == null) {
                return null;
            }
            if (String.IsNullOrEmpty(encrpytedString)) {
                return String.Empty;
            }
            var encrpytedByteArray = Convert.FromBase64String(encrpytedString);
            var decryptedByteArray = ProtectedData.Unprotect(encrpytedByteArray, StringToBytes(_entropy), DataProtectionScope.CurrentUser);
            return BytesToString(decryptedByteArray);

        }

        public static void SetEncryptedValue(this ISettings settings, string section, string key, string value) {
            if (String.IsNullOrEmpty(section)) {
                throw new ArgumentException("Argument cannot be null or empty.", "section");
            }
            if (String.IsNullOrEmpty(key)) {
                throw new ArgumentException("Argument cannot be null or empty.", "key");
            }
            if (value == null) {
                throw new ArgumentNullException("value");
            }

            if (String.IsNullOrEmpty(value)) {
                settings.SetValue(section, key, String.Empty);
            }
            else {
                var decryptedByteArray = StringToBytes(value);
                var encryptedByteArray = ProtectedData.Protect(decryptedByteArray, StringToBytes(_entropy), DataProtectionScope.CurrentUser);
                var encryptedString = Convert.ToBase64String(encryptedByteArray);
                settings.SetValue(section, key, encryptedString);
            }

        }

        private static byte[] StringToBytes(string str) {
            return Encoding.UTF8.GetBytes(str);
        }

        private static string BytesToString(byte[] bytes) {
            return Encoding.UTF8.GetString(bytes);
        }
    }
}
