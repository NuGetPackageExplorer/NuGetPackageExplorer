﻿using System.Collections.Generic;
using System.Threading.Tasks;
using NuGet.Packaging.Signing;

namespace NuGetPe
{
    public interface ISignaturePackage : IPackage
    {
        bool IsSigned { get; }
        bool IsVerified { get; }
        SignatureInfo PublisherSignature { get; }
        IReadOnlyList<SignatureInfo> RepositorySignatures { get; }
        VerifySignaturesResult VerificationResult { get; }
        string Source { get; }
        Task LoadSignatureDataAsync();
        Task VerifySignatureAsync();
    }
}