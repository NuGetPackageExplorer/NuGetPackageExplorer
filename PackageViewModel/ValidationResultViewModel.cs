using System.Globalization;
using System.Resources;
using System.Text;

using NuGet.Common;
using NuGet.Packaging.Signing;

using CI = System.Globalization.CultureInfo;


namespace PackageExplorerViewModel
{
    public sealed class ValidationResultViewModel
    {
        private readonly VerifySignaturesResult _verifySignaturesResult;
        private static ResourceManager resManager => Resources.ResourceManager;
        private static CultureInfo cultureInfo => CultureInfo.CurrentCulture;

        public ValidationResultViewModel(VerifySignaturesResult verifySignaturesResult)
        {
            _verifySignaturesResult = verifySignaturesResult ?? throw new ArgumentNullException(nameof(verifySignaturesResult));

            Trust = verifySignaturesResult.Results.Select(static r => r.Trust).Min();

            ErrorIssues = verifySignaturesResult.Results.SelectMany(static prv => prv.GetErrorIssues()).ToList();
            WarningIssues = verifySignaturesResult.Results.SelectMany(static prv => prv.GetWarningIssues()).ToList();
            InformationIssues = verifySignaturesResult.Results
                                                      .SelectMany(static prv => prv.Issues)
                                                      .Where(static sl => sl.Level == LogLevel.Information)
                                                      .ToList();
        }

        public string ValidationSummary
        {
            get
            {
                var messageBuilder = new StringBuilder(); 
                messageBuilder.AppendLine(CI.CurrentCulture, $"Validation Result: {Valid}");
                messageBuilder.AppendLine(CI.CurrentCulture, $"Signed: {Signed}");
                messageBuilder.AppendLine(CI.CurrentCulture, $"Trust Level: {Trust}");

                if (ErrorIssues.Count > 0)
                {
                    messageBuilder.AppendLine(resManager.GetString("ValidationResult_Errors", cultureInfo));
                    foreach (var issue in ErrorIssues)
                    {
                        messageBuilder.AppendLine(issue.Message);
                    }
                }

                if (WarningIssues.Count > 0)
                {
                    messageBuilder.AppendLine(resManager.GetString("ValidationResult_Warnings", cultureInfo));
                    foreach (var issue in WarningIssues)
                    {
                        messageBuilder.AppendLine(resManager.GetString("ValidationResult_Info", cultureInfo));
                    }
                }

                if (InformationIssues.Count > 0)
                {
                    messageBuilder.AppendLine("Information:");
                    foreach (var issue in InformationIssues)
                    {
                        messageBuilder.AppendLine(issue.Message);
                    }
                }

                return messageBuilder.ToString();
            }
            
        }


        public bool Valid => _verifySignaturesResult.IsValid;
#pragma warning disable CA1720 // Identifier contains type name
        public bool Signed => _verifySignaturesResult.IsSigned;
#pragma warning restore CA1720 // Identifier contains type name


        public IReadOnlyList<ILogMessage> ErrorIssues { get; }
        public IReadOnlyList<ILogMessage> WarningIssues { get; }
        public IReadOnlyList<ILogMessage> InformationIssues { get; }

        public SignatureVerificationStatus Trust { get; }
    }
}
