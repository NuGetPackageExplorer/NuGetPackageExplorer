using System;

using NuGetPe;

namespace PackageExplorerViewModel
{
    public sealed class SymbolValidatorResultViewModel
    {
        private readonly SymbolValidatorResult? _result;

        public SymbolValidatorResultViewModel(SymbolValidatorResult? symbolValidatorResult)
        {
            _result = symbolValidatorResult;
        }

        public SymbolValidationResult SourceLinkResult => _result?.SourceLinkResult ?? SymbolValidationResult.NothingToValidate;
        public string? SourceLinkErrorMessage => _result?.SourceLinkErrorMessage;

        public DeterministicResult DeterministicResult => _result?.DeterministicResult ?? DeterministicResult.NothingToValidate;
        public string? DeterministicErrorMessage => _result?.DeterministicErrorMessage;

        public HasCompilerFlagsResult CompilerFlagsResult => _result?.CompilerFlagsResult ?? HasCompilerFlagsResult.NothingToValidate;
        public string? CompilerFlagsMessage => _result?.CompilerFlagsMessage;

        public Exception? Exception => _result?.Exception;
    }
}
