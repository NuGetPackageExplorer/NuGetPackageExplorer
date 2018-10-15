using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using AuthenticodeExaminer;

namespace NuGetPackageExplorer.Types
{
    public class AuthenticodeSignature
    {
        private readonly ISignature _signature;

        public AuthenticodeSignature(ISignature signature)
        {
            _signature = signature ?? throw new ArgumentNullException(nameof(signature));
        }

        public PublisherInformation PublisherInformation { get; private set; }
    }
}
