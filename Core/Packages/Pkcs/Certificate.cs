using System;
using System.Collections.Generic;
using System.Formats.Asn1;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

// docs reference: https://tools.ietf.org/html/rfc5280
// impl reference: n/a

namespace NuGetPe.Packages.Pkcs
{
    public class Certificate
    {
        public TBSCertificate TbsCertificate { get; private set; }
        public AlgorithmIdentifier SignatureAlgorithm { get; private set; }
        public ReadOnlyMemory<byte> SignatureValue { get; private set; }

        public static Certificate Decode(AsnReader reader)
        {
            // https://tools.ietf.org/html/rfc5280
            /*  Certificate  ::=  SEQUENCE  {
                    tbsCertificate       TBSCertificate,
                    signatureAlgorithm   AlgorithmIdentifier,
                    signatureValue       BIT STRING  }*/
            var sequence = reader.ReadSequence();

            var tbsCertificate = TBSCertificate.Decode(sequence);
            var signatureAlgorithm = AlgorithmIdentifier.Decode(sequence);
            var signatureValue = sequence.ReadEncodedValue();

            sequence.ThrowIfNotEmpty();
            return new()
            {
                TbsCertificate = tbsCertificate,
                SignatureAlgorithm = signatureAlgorithm,
                SignatureValue = signatureValue,
            };
        }
    }
}
