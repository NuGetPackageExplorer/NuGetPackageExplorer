using System;
using System.Net;

namespace NuGet
{
    public class NullCredentialProvider : ICredentialProvider
    {
        private static readonly NullCredentialProvider _instance = new NullCredentialProvider();

        private NullCredentialProvider()
        {
        }

        public static ICredentialProvider Instance
        {
            get { return _instance; }
        }

        #region ICredentialProvider Members

        public ICredentials GetCredentials(Uri uri, IWebProxy proxy, CredentialType credentialType, bool retrying)
        {
            return null;
        }

        #endregion
    }
}