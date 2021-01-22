using System;

namespace NuGetPe
{
    public class UsageException : Exception
    {
        public UsageException(string message) : base(message)
        {
        }
    }
}
