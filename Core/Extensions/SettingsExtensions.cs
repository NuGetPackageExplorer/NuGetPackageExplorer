using System;
using System.Security.Cryptography;
using System.Text;

namespace NuGetPe
{
    public static class SettingsExtensions
    {
        private const string Entropy = "NuGet";

        public static string GetDecryptedValue(this ISettings settings, string section, string key)
        {
            if (String.IsNullOrEmpty(section))
            {
                throw new ArgumentException("Argument cannot be null or empty.", "section");
            }

            if (String.IsNullOrEmpty(key))
            {
                throw new ArgumentException("Argument cannot be null or empty.", "key");
            }

            string encrpytedString = settings.GetValue(section, key);
            if (encrpytedString == null)
            {
                return null;
            }
            if (String.IsNullOrEmpty(encrpytedString))
            {
                return String.Empty;
            }
            byte[] encrpytedByteArray = Convert.FromBase64String(encrpytedString);
            byte[] decryptedByteArray = ProtectedData.Unprotect(encrpytedByteArray, StringToBytes(Entropy),
                                                                DataProtectionScope.CurrentUser);
            return BytesToString(decryptedByteArray);
        }

        public static void SetEncryptedValue(this ISettings settings, string section, string key, string value)
        {
            if (String.IsNullOrEmpty(section))
            {
                throw new ArgumentException("Argument cannot be null or empty.", "section");
            }
            if (String.IsNullOrEmpty(key))
            {
                throw new ArgumentException("Argument cannot be null or empty.", "key");
            }
            if (value == null)
            {
                throw new ArgumentNullException("value");
            }

            if (String.IsNullOrEmpty(value))
            {
                settings.SetValue(section, key, String.Empty);
            }
            else
            {
                byte[] decryptedByteArray = StringToBytes(value);
                byte[] encryptedByteArray = ProtectedData.Protect(decryptedByteArray, StringToBytes(Entropy),
                                                                  DataProtectionScope.CurrentUser);
                string encryptedString = Convert.ToBase64String(encryptedByteArray);
                settings.SetValue(section, key, encryptedString);
            }
        }

        private static byte[] StringToBytes(string str)
        {
            return Encoding.UTF8.GetBytes(str);
        }

        private static string BytesToString(byte[] bytes)
        {
            return Encoding.UTF8.GetString(bytes);
        }
    }
}