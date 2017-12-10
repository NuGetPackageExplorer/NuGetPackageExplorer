using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using NuGet.Common;
using NuGet.Packaging;
using NuGet.Packaging.Signing;


namespace PackageExplorerViewModel
{
    public sealed class ValidationResultViewModel : INotifyPropertyChanged
    {
        private readonly VerifySignaturesResult verifySignaturesResult;

        public ValidationResultViewModel(VerifySignaturesResult verifySignaturesResult)
        {
            this.verifySignaturesResult = verifySignaturesResult ?? throw new ArgumentNullException(nameof(verifySignaturesResult));

            Trust = verifySignaturesResult.Results.Select(r => r.Trust).Min();

            ErrorIssues = verifySignaturesResult.Results.SelectMany(prv => prv.GetErrorIssues()).ToList();
            WarningIssues = verifySignaturesResult.Results.SelectMany(prv => prv.GetWarningIssues()).ToList();
            InformationIssues = verifySignaturesResult.Results.SelectMany(prv => prv.Issues).Where(sl => sl.Level == LogLevel.Information).Select(sl => sl.ToLogMessage()).ToList();
        }


        public bool Valid => verifySignaturesResult.Valid;


        public IReadOnlyList<ILogMessage> ErrorIssues { get; }
        public IReadOnlyList<ILogMessage> WarningIssues { get; }
        public IReadOnlyList<ILogMessage> InformationIssues { get; }

        public SignatureVerificationStatus Trust { get; }

        public event PropertyChangedEventHandler PropertyChanged;

        
        private void RaisePropertyChange(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}