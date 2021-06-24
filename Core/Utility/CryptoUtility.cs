using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

using NuGet.Common;
using NuGet.Packaging;
using NuGet.Packaging.Signing;

using NuGetPe.Packages.Pkcs;

using PeNet;

namespace NuGetPe.Utility
{
    public static class CryptoUtility
    {
        public static TBSCertificate? GetSigningCertificate(PeFile peFile)
        {
            if (peFile == null) throw new ArgumentNullException(nameof(peFile));

            if (peFile.WinCertificate?.WCertificateType != PeNet.Header.Pe.WinCertificateType.PkcsSignedData)
            {
                return null;
            }

            var pkcs7 = peFile.WinCertificate.BCertificate.ToArray();

            const string SignedDataOid = "1.2.840.113549.1.7.2";
            var content = ContentInfo.Decode(pkcs7);
            if (content.ContentType != SignedDataOid)
            {
                throw new CryptographicException("Cryptography_Cms_InvalidMessageType");
            }

            var data = SignedData.Decode(content.Content);

            var signerSerialNumber = data.SignerInfos
                ?.ElementAtOrDefault(0)
                ?.Sid?.IssuerAndSerialNumber?.SerialNumber;
            if (signerSerialNumber == null)
            {
                return null;
            }

            var certificates = data.Certificates
                ?.Select(x => x?.Certificate?.TbsCertificate)
                .Where(x => x?.SerialNumber is { } sn && sn == signerSerialNumber)
                .ToArray();
            var signingCertificate = certificates?.Length switch
            {
                1 => certificates[0],
                _ => throw new CryptographicException($"Expected to find one certificate with serial number '{signerSerialNumber}' but found {certificates?.Length ?? 0}.")
            };

            return signingCertificate;
        }

        public static (SignatureInfo? PublisherSignature, RepositorySignatureInfo? RepositorySignature) GetSignatures(PackageArchiveReader reader)
        {
            if (reader == null) throw new ArgumentNullException(nameof(reader));

            var entry = reader.GetEntry(SigningSpecifications.V1.SignaturePath);
            using var stream = entry.Open();

            var data = ContentInfo.Unwrap(SignedData.ContentTypeID, stream.ReadAllBytes(), SignedData.Decode);
            if (data.SignerInfos.ElementAtOrDefault(0) is not { } signer) return default;
            if (GetSignerCertificate(signer, data.Certificates) is not { } certificate) return default;
            var ts = GetTimestamps(signer)?.FirstOrDefault();

            var signatureType = AttributeUtility2.GetSignatureType(signer.SignedAttrs);
            if (signatureType == SignatureType.Author)
            {
                var publisherSignature = new PublisherSignatureInfo(certificate, ts?.Value, ts?.SignerCertificate);

                // Author signatures must be the primary, but they can contain a repository counter signature
                var unsignedAttrs = signer.UnsignedAttrs;
                var counterSignatures = AttributeUtility2.GetCounterSigner(unsignedAttrs);
                var repositoryCounterSignatures = counterSignatures
                    ?.Where(x => AttributeUtility2.GetSignatureType(x.SignedAttrs) == SignatureType.Repository)
                    .ToArray();
                var repositoryCounterSignature = repositoryCounterSignatures?.Length switch
                {
                    0 => default,
                    1 => repositoryCounterSignatures[0],
                    _ => throw new SignatureException(NuGetLogCode.NU3032, "Error_NotOneRepositoryCounterSignature"),
                };
                var repositorySignature = GetRepositorySignature();

                return (publisherSignature, repositorySignature);

                RepositorySignatureInfo? GetRepositorySignature()
                {
                    if (repositoryCounterSignature is null) return null;

                    var certificate = GetSignerCertificate(repositoryCounterSignature, data.Certificates);
                    var ts = GetTimestamps(repositoryCounterSignature)?.FirstOrDefault();
                    var url = AttributeUtility2.GetNuGetV3ServiceIndexUrl(repositoryCounterSignature.SignedAttrs);
                    var owners = AttributeUtility2.GetNuGetPackageOwners(repositoryCounterSignature.SignedAttrs);

                    return new RepositorySignatureInfo(certificate, ts?.Value, ts?.SignerCertificate, url, owners);
                }
            }
            else if (signatureType == SignatureType.Repository)
            {
                var url = AttributeUtility2.GetNuGetV3ServiceIndexUrl(signer.SignedAttrs);
                var owners = AttributeUtility2.GetNuGetPackageOwners(signer.SignedAttrs);

                var repositorySignature = new RepositorySignatureInfo(certificate, ts?.Value, ts?.SignerCertificate, url, owners);
                return (null, repositorySignature);
            }
            else
            {
                return default;
            }
        }

        private static TBSCertificate? GetSignerCertificate(SignerInfo signerInfo, CertificateChoices[]? certificates)
        {
            if (signerInfo is null) throw new ArgumentNullException(nameof(signerInfo));
            if (certificates is null) return null;

            if (signerInfo.Sid.IssuerAndSerialNumber is { } iasn)
            {
                return certificates
                    .Select(x => x?.Certificate?.TbsCertificate)
                    .FirstOrDefault(x => x?.Issuer == iasn.Issuer && x?.SerialNumber == iasn.SerialNumber);
            }
            else if (signerInfo.Sid.SubjectKeyIdentifier is { } keyIdentifier)
            {
                return certificates
                    .Select(x => x?.Certificate?.TbsCertificate)
                    .FirstOrDefault(x => x?.Extensions
                        ?.FirstOrDefault(y => y.ExtnID == Oids.SubjectKeyIdentifier)
                        ?.GetSubjectKeyIdentifier().Span.SequenceEqual(keyIdentifier.Span) == true);
            }

            return null;
        }

        private static IEnumerable<(DateTimeOffset Value, TBSCertificate? SignerCertificate)?>? GetTimestamps(SignerInfo? signerInfo)
        {
            return signerInfo?.UnsignedAttrs
                ?.Where(x => x.AttrType == Rfc3161TstInfo.ContentTypeID)
                .SelectMany(x => x.AttrValues)
                .Select(x =>
                {
                    var data = ContentInfo.Unwrap(SignedData.ContentTypeID, x, SignedData.Decode);
                    var info = Rfc3161TstInfo.From(data.EncapContentInfo);
                    var certificate = data.SignerInfos.ElementAtOrDefault(0) is { } signer
                        ? GetSignerCertificate(signer, data.Certificates)
                        : null;

                    return (info.Gentime, certificate);
                })
                .CastAsNullable();
        }
    }
}
