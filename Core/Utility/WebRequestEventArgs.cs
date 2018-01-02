using System;
using System.Net;

namespace NuGetPe
{
    public class WebRequestEventArgs : EventArgs
    {
        public WebRequestEventArgs(WebRequest request)
        {
            Request = request ?? throw new ArgumentNullException("request");
        }

        public WebRequest Request { get; private set; }
    }
}