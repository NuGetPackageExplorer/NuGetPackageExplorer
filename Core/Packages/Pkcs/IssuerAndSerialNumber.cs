using System;
using System.Collections.Generic;
using System.Formats.Asn1;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;

// docs reference: https://tools.ietf.org/html/rfc5652
// impl reference: https://github.com/dotnet/runtime/tree/b02e13abab3d6f499751af2ad1ae6333fb4e633d/src/libraries/System.Security.Cryptography.Pkcs/src
//      ^ modified

namespace NuGetPe.Packages.Pkcs
{
    public class IssuerAndSerialNumber
    {
        public DistinguishedName Issuer { get; private set; }
        public BigInteger SerialNumber { get; private set; }

        public static IssuerAndSerialNumber Decode(AsnReader reader)
        {
            /*  IssuerAndSerialNumber ::= SEQUENCE {
                    issuer Name,
                    serialNumber CertificateSerialNumber }
                CertificateSerialNumber ::= INTEGER */
            var sequence = reader.ReadSequence();
            var issuer = DistinguishedName.Decode(sequence);
            var serialNumber = sequence.ReadInteger();

            sequence.ThrowIfNotEmpty();
            return new()
            {
                Issuer = issuer,
                SerialNumber = serialNumber,
            };
        }
    }
}
