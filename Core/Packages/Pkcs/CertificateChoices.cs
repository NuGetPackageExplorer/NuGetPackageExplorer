using System;
using System.Collections.Generic;
using System.Formats.Asn1;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

// docs reference: https://tools.ietf.org/html/rfc5652
// impl reference: n/a

namespace NuGetPe.Packages.Pkcs
{
    public class CertificateChoices
    {
        public Certificate Certificate { get; private set; }

        public static CertificateChoices Decode(AsnReader reader, bool validateEmpty = false)
        {
            // The CertificateChoices type gives either
            // - a PKCS #6 extended certificate [PKCS#6], 
            // - an X.509 certificate,
            // - a version 1 X.509 attribute certificate (ACv1) [X.509-97], 
            // - a version 2 X.509 attribute certificate (ACv2) [X.509-00], 
            // - or any other certificate format.
            /*  CertificateChoices ::= CHOICE {
                    certificate Certificate,
                    extendedCertificate [0] IMPLICIT ExtendedCertificate,  -- Obsolete
                    v1AttrCert [1] IMPLICIT AttributeCertificateV1,        -- Obsolete
                    v2AttrCert [2] IMPLICIT AttributeCertificateV2,
                    other [3] IMPLICIT OtherCertificateFormat } */
            var tag = reader.PeekTag();

            if (tag == Asn1Tag.Sequence)
            {
                var result = new CertificateChoices()
                {
                    Certificate = Certificate.Decode(reader),
                };

                if (validateEmpty) reader.ThrowIfNotEmpty();
                return result;
            }

            throw new NotImplementedException("CertificateChoices");
        }
    }
}
