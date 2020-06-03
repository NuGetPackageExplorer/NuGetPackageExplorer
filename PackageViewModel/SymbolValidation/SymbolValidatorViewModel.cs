using System;
using System.ComponentModel;
using NuGetPe;

namespace PackageExplorerViewModel
{
    public class SymbolValidatorViewModel : INotifyPropertyChanged
    {
        private readonly SymbolValidator _symbolValidator;

        public SymbolValidatorViewModel(PackageViewModel packageViewModel, SymbolValidator symbolValidator)
        {
            if (packageViewModel == null) throw new ArgumentNullException(nameof(packageViewModel));
            packageViewModel.PropertyChanged += _packageViewModel_PropertyChanged;
            _symbolValidator = symbolValidator ?? throw new ArgumentNullException(nameof(symbolValidator));

            SourceLinkResult = SymbolValidationResult.Pending;
            DeterministicResult = DeterministicResult.Pending;
        }

        private void _packageViewModel_PropertyChanged(object? sender, PropertyChangedEventArgs e)
        {
            if(e.PropertyName == null)
            {
                SourceLinkResult = SymbolValidationResult.Pending;
                SourceLinkErrorMessage = null;
                DeterministicResult = DeterministicResult.Pending;
                DeterministicErrorMessage = null;

                Refresh();
            }
        }

        public event PropertyChangedEventHandler? PropertyChanged;

        public async void Refresh()
        {
            try
            {
                (SourceLinkResult, SourceLinkErrorMessage, DeterministicResult, DeterministicErrorMessage) = await _symbolValidator.Validate().ConfigureAwait(true);
            }
            catch(Exception e)
            {
                DiagnosticsClient.TrackException(e);
            }
            finally
            {
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(SourceLinkResult)));
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(SourceLinkErrorMessage)));
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(DeterministicResult)));
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(DeterministicErrorMessage)));
            }
        }

        public SymbolValidationResult SourceLinkResult
        {
            get; private set;
        }

        public string? SourceLinkErrorMessage
        {
            get; private set;
        }

        public DeterministicResult DeterministicResult
        {
            get; private set;
        }

        public string? DeterministicErrorMessage
        {
            get; private set;
        }
    }
}
