using System;
using System.Collections.Generic;
using System.Formats.Asn1;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using NuGet.Packaging.Signing;

using NuGetPe.Packages.Pkcs;

using CommitmentTypeIndication = NuGetPe.Packages.Pkcs.CommitmentTypeIndication;

namespace NuGetPe.Utility
{
    public static class AttributeUtility2
    {
        public static SignatureType GetSignatureType(AttributeValue[]? signedAttrs)
        {
            var types = signedAttrs
                ?.Where(attr => attr.AttrType == Oids.CommitmentTypeIndication)
                .SelectMany(attr => attr.AttrValues)
                .Select(value => CommitmentTypeIndication.Decode(value))
                .Select(indication => GetSignatureType(indication.CommitmentTypeId))
                .Distinct()
                .Where(x => x != SignatureType.Unknown)
                .ToArray();
            var type = types?.Length switch
            {
                // Remove unknown values, these could be future values.
                null => SignatureType.Unknown,
                0 => SignatureType.Unknown,
                1 => types[0],
                // Author and repository values are mutually exclusive in the same signature.
                // If multiple distinct known values exist then the attribute is invalid.
                _ => throw new SignatureException("CommitmentTypeIndicationAttributeInvalidCombination"),
            };

            return type;
        }

        public static SignatureType GetSignatureType(string oid)
        {
            return oid switch
            {
                Oids.CommitmentTypeIdentifierProofOfOrigin => SignatureType.Author,
                Oids.CommitmentTypeIdentifierProofOfReceipt => SignatureType.Repository,
                _ => SignatureType.Unknown,
            };
        }

        public static Uri? GetNuGetV3ServiceIndexUrl(AttributeValue[]? signedAttrs)
        {
            const string AttributeName = "nuget-v3-service-index-url";

            var attribute = signedAttrs?.GetAttribute(Oids.NuGetV3ServiceIndexUrl);
            if (attribute == null)
            {
                throw new SignatureException($"ExactlyOneAttributeRequired: {AttributeName}");
            }
            if (attribute.AttrValues.Length != 1)
            {
                throw new SignatureException($"ExactlyOneAttributeValueRequired: {AttributeName}");
            }

            var url = NuGetV3ServiceIndexUrl.Read(attribute.AttrValues[0].ToArray());

            return url.V3ServiceIndexUrl;
        }

        public static IReadOnlyList<string>? GetNuGetPackageOwners(AttributeValue[]? signedAttrs)
        {
            const string AttributeName = "nuget-package-owners";

            var attribute = signedAttrs?.GetAttribute(Oids.NuGetPackageOwners);
            if (attribute == null) return null;
            if (attribute.AttrValues.Length != 1)
            {
                throw new SignatureException($"ExactlyOneAttributeValueRequired: {AttributeName}");
            }

            var owners = NuGetPackageOwners.Read(attribute.AttrValues[0].ToArray());

            return owners.PackageOwners;
        }

        public static SignerInfo[]? GetCounterSigner(AttributeValue[]? unsignedAttrs)
        {
            return unsignedAttrs
                ?.Where(x => x.AttrType == Oids.Countersignature)
                .SelectMany(attr => attr.AttrValues)
                .Select(x => SignerInfo.Decode(x))
                .ToArray();
        }
    }
}
