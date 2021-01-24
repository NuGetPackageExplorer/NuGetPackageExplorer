using System;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

using NuGet.Versioning;

namespace NuGetPe
{
    internal class Program
    {
        // Standard exit codes, see https://man.openbsd.org/sysexits and https://docs.microsoft.com/en-us/cpp/c-runtime-library/exit-success-exit-failure
        // ReSharper disable InconsistentNaming
        private const int EXIT_SUCCESS   =  0;
        private const int EXIT_FAILURE   =  1;
        private const int EX_USAGE       = 64; // The command was used incorrectly, e.g., with the wrong number of arguments, a bad flag, bad syntax in a parameter, or whatever.
        private const int EX_UNAVAILABLE = 69; // A service is unavailable. This can occur if a support program or file does not exist. This can also be used as a catch-all message when something you wanted to do doesn't work, but you don't know why.
        private const int EX_SOFTWARE    = 70; // An internal software error has been detected. This should be limited to non-operating system related errors if possible.
        // ReSharper restore InconsistentNaming

        private static async Task<int> Main(string[] args)
        {
            try
            {
                using var cancellationTokenSource = new CancellationTokenSource();
                Console.CancelKeyPress += (_, eventArgs) =>
                {
                    // ReSharper disable AccessToDisposedClosure
                    eventArgs.Cancel = !cancellationTokenSource.IsCancellationRequested;
                    cancellationTokenSource.Cancel();
                    // ReSharper restore AccessToDisposedClosure
                };

                var (packageId, packageVersion) = ParseArguments(args);

                var packageFile = new FileInfo(packageId);
                if (packageFile.Exists)
                {
                    await Console.Out.WriteLineAsync($"Validating {packageFile.FullName}").ConfigureAwait(false);
                }
                else
                {
                    using var downloader = new NuGetPackageDownloader(Console.Out);
                    packageFile = await downloader.DownloadAsync(packageId, packageVersion, cancellationTokenSource.Token).ConfigureAwait(false);
                    var versionString = packageVersion == null ? "" : packageVersion.ToFullString() + " ";
                    await Console.Out.WriteLineAsync($"Validating {packageId} {versionString}from {packageFile.FullName}").ConfigureAwait(false);
                }

                var isValid = await RunAsync(packageFile, cancellationTokenSource.Token).ConfigureAwait(false);

                return isValid ? EXIT_SUCCESS : EXIT_FAILURE;
            }
            catch (UsageException exception)
            {
                await Console.Error.WriteLineAsync(exception.Message).ConfigureAwait(false);
                return EX_USAGE;
            }
            catch (UnavailableException exception)
            {
                await Console.Error.WriteLineAsync(exception.Message).ConfigureAwait(false);
                return EX_UNAVAILABLE;
            }
            catch (Exception exception)
            {
                await Console.Error.WriteLineAsync(exception.ToString()).ConfigureAwait(false);
                return EX_SOFTWARE;
            }
        }

        private static (string packageId, NuGetVersion? packageVersion) ParseArguments(string[] args)
        {
            if (!args.Any())
            {
                throw new UsageException("usage: nuget-package-validate package-id [package-version]");
            }

            var packageId = args[0];
            if (args.Length < 2)
            {
                return (packageId, null);
            }

            if (!(NuGetVersion.TryParse(args[1], out var packageVersion)))
            {
                throw new UsageException($"The specified version ({args[1]}) is not a valid NuGet version. See https://docs.microsoft.com/en-us/nuget/concepts/package-versioning for more information.");
            }

            return (packageId, packageVersion);
        }

        private static async Task<bool> RunAsync(FileInfo packageFile, CancellationToken cancellationToken)
        {
            using var package = new ZipPackage(packageFile.FullName);
            
            var validator = new SymbolValidator(package, packageFile.FullName);
            var result = await validator.Validate(cancellationToken).ConfigureAwait(false);
            await WriteResult("Source Link", result.SourceLinkResult, result.SourceLinkErrorMessage, SourceLinkDescription).ConfigureAwait(false);
            await WriteResult("Deterministic (dll/exe)", result.DeterministicResult, result.DeterministicErrorMessage, DeterministicDescription).ConfigureAwait(false);
            await WriteResult("Compiler Flags", result.CompilerFlagsResult, result.CompilerFlagsMessage, CompilerFlagsDescription).ConfigureAwait(false);
            var sourceLinkValid = result.SourceLinkResult is SymbolValidationResult.Valid or SymbolValidationResult.ValidExternal or SymbolValidationResult.NothingToValidate;
            var deterministicValid = result.DeterministicResult is DeterministicResult.Valid or DeterministicResult.NothingToValidate;
            var compilerFlagsValid = result.CompilerFlagsResult is HasCompilerFlagsResult.Present or HasCompilerFlagsResult.NothingToValidate;
            return sourceLinkValid && deterministicValid && compilerFlagsValid;
        }

        private static async Task WriteResult<T>(string description, T value, string? errorMessage, Func<T, string>? enumDescription)
        {
            string errorString;
            if (errorMessage != null)
            {
                const int indent = 4;
                errorString = Environment.NewLine + string.Join(Environment.NewLine, errorMessage.Split(Environment.NewLine, StringSplitOptions.RemoveEmptyEntries).Select(e => new string(' ', indent) + e));
            }
            else
            {
                errorString = "";
            }
            await Console.Out.WriteLineAsync($"• {description}: {enumDescription?.Invoke(value) ?? value?.ToString()}{errorString}{Environment.NewLine}").ConfigureAwait(false);
        }

        private static string SourceLinkDescription(SymbolValidationResult result)
        {
            return result switch
            {
                SymbolValidationResult.Valid => "✅ Valid",
                SymbolValidationResult.ValidExternal => "✅ Valid with Symbol Server",
                SymbolValidationResult.InvalidSourceLink => "❌ Invalid",
                SymbolValidationResult.NoSourceLink => "❌ Has Symbols, No Source Link",
                SymbolValidationResult.NoSymbols => "❌ Missing Symbols",
                SymbolValidationResult.NothingToValidate => "✅ No files found to validate",
                SymbolValidationResult.HasUntrackedSources => "⚠️ Contains untracked sources (obj)",
                _ => throw new ArgumentOutOfRangeException(nameof(result), result, $@"The value of argument '{nameof(result)}' ({result}) is invalid for enum type '{nameof(SymbolValidationResult)}'.")
            };
        }

        private static string DeterministicDescription(DeterministicResult result)
        {
            return result switch
            {
                DeterministicResult.Valid => "✅ Valid",
                DeterministicResult.NonDeterministic => "❌ Non deterministic",
                DeterministicResult.NothingToValidate => "✅ No files found to validate",
                DeterministicResult.HasUntrackedSources => "⚠️ Contains untracked sources (obj)",
                _ => throw new ArgumentOutOfRangeException(nameof(result), result, $@"The value of argument '{nameof(result)}' ({result}) is invalid for enum type '{nameof(DeterministicResult)}'.")
            };
        }

        private static string CompilerFlagsDescription(HasCompilerFlagsResult result)
        {
            return result switch
            {
                HasCompilerFlagsResult.Present => "✅ Present",
                HasCompilerFlagsResult.Missing => "❌ Missing",
                HasCompilerFlagsResult.NothingToValidate => "✅ No files found to validate",
                _ => throw new ArgumentOutOfRangeException(nameof(result), result, $@"The value of argument '{nameof(result)}' ({result}) is invalid for enum type '{nameof(HasCompilerFlagsResult)}'.")
            };
        }
    }
}
