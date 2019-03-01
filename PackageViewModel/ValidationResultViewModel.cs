using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using NuGet.Common;
using NuGet.Packaging.Signing;


namespace PackageExplorerViewModel
{
    public sealed class ValidationResultViewModel
    {
        private readonly VerifySignaturesResult _verifySignaturesResult;

        public ValidationResultViewModel(VerifySignaturesResult verifySignaturesResult)
        {
            _verifySignaturesResult = verifySignaturesResult ?? throw new ArgumentNullException(nameof(verifySignaturesResult));

            Trust = verifySignaturesResult.Results.Select(r => r.Trust).Min();

            ErrorIssues = verifySignaturesResult.Results.SelectMany(prv => prv.GetErrorIssues()).ToList();
            WarningIssues = verifySignaturesResult.Results.SelectMany(prv => prv.GetWarningIssues()).ToList();
            InformationIssues = verifySignaturesResult.Results
                                                      .SelectMany(prv => prv.Issues)
                                                      .Where(sl => sl.Level == LogLevel.Information)
                                                      .ToList();
        }


        public bool Valid => _verifySignaturesResult.IsValid;
        public bool Signed => _verifySignaturesResult.IsSigned;


        public IReadOnlyList<ILogMessage> ErrorIssues { get; }
        public IReadOnlyList<ILogMessage> WarningIssues { get; }
        public IReadOnlyList<ILogMessage> InformationIssues { get; }

        public SignatureVerificationStatus Trust { get; }
    }
}
