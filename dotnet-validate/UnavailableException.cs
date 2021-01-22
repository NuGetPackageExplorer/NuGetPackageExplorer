using System;

namespace NuGetPe
{
    public class UnavailableException : Exception
    {
        public UnavailableException(string message) : base(message)
        {
        }
    }
}
