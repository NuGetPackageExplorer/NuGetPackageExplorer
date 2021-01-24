using System;

namespace NuGetPe
{
    public class UsageException : Exception
    {
        public UsageException(string message) : base(message)
        {
        }

        public UsageException()
        {
        }

        public UsageException(string message, Exception innerException) : base(message, innerException)
        {
        }
    }
}
