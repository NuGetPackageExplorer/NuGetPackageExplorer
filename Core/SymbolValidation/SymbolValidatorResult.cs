using System;

namespace NuGetPe
{
    public enum SymbolValidationResult
    {
        /// <summary>
        /// Everything checks out and symbols are in the package
        /// </summary>
        Valid,

        /// <summary>
        /// Valid with symbol servers
        /// </summary>
        ValidExternal,

        /// <summary>
        /// Source Link exists but has errors
        /// </summary>
        InvalidSourceLink,

        /// <summary>
        /// Missing source link data
        /// </summary>
        NoSourceLink,

        /// <summary>
        /// No symbols found
        /// </summary>
        NoSymbols,

        /// <summary>
        /// No relevant files to validate.
        /// </summary>
        NothingToValidate,

        /// <summary>
        /// Valid/ValidExternal except contains untracked sources
        /// </summary>
        HasUntrackedSources
    }

    public enum DeterministicResult
    {
        /// <summary>
        /// Assembly and sources are deterministic
        /// </summary>
        Valid,

        /// <summary>
        /// Source and assembly are not deterministic
        /// </summary>
        NonDeterministic,

        /// <summary>
        /// No relevant files to validate.
        /// </summary>
        NothingToValidate,

        /// <summary>
        /// Valid but has untracked sources
        /// </summary>
        HasUntrackedSources
    }

    public enum HasCompilerFlagsResult
    {
        /// <summary>
        /// Symbols have compiler flag data, but too old to be reproducible
        /// </summary>
        Present,

        /// <summary>
        /// Symbols do not have compiler flag data
        /// </summary>
        Missing,

        /// <summary>
        /// Symbols have compiler flag data and are recent enough for reproducible builds
        /// </summary>
        Valid,

        /// <summary>
        /// No relevant files to validate.
        /// </summary>
        NothingToValidate
    }

    public class SymbolValidatorResult
    {
        public SymbolValidatorResult(
            SymbolValidationResult sourceLinkResult, string? sourceLinkErrorMessage,
            DeterministicResult deterministicResult, string? deterministicErrorMessage,
            HasCompilerFlagsResult compilerFlagsResult, string? compilerFlagsMessage,
            Exception? exception = null)
        {
            SourceLinkResult = sourceLinkResult;
            SourceLinkErrorMessage = sourceLinkErrorMessage;
            DeterministicResult = deterministicResult;
            DeterministicErrorMessage = deterministicErrorMessage;
            CompilerFlagsResult = compilerFlagsResult;
            CompilerFlagsMessage = compilerFlagsMessage;

            Exception = exception;
        }

        public SymbolValidationResult SourceLinkResult { get; }
        public string? SourceLinkErrorMessage { get; }

        public DeterministicResult DeterministicResult { get; }
        public string? DeterministicErrorMessage { get; }

        public HasCompilerFlagsResult CompilerFlagsResult { get; }
        public string? CompilerFlagsMessage { get; }

        public Exception? Exception { get; }
    }
}
