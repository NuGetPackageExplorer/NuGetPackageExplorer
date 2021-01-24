using System;

namespace NuGetPe
{
    public class UnavailableException : Exception
    {
        public UnavailableException(string message) : base(message)
        {
        }

        public UnavailableException()
        {
        }

        public UnavailableException(string message, Exception innerException) : base(message, innerException)
        {
        }
    }
}
