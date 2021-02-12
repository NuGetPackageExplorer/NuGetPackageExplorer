using System;
using System.CommandLine;
using System.CommandLine.Invocation;
using System.CommandLine.Parsing;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.FileSystemGlobbing;
using Microsoft.Extensions.FileSystemGlobbing.Abstractions;
using NuGet.Versioning;

namespace NuGetPe
{
    internal class Program
    {
        // Standard exit codes, see https://man.openbsd.org/sysexits and https://docs.microsoft.com/en-us/cpp/c-runtime-library/exit-success-exit-failure
        
        private const int EXIT_SUCCESS   =  0;
        private const int EXIT_FAILURE   =  1;
        private const int EX_UNAVAILABLE = 69; // A service is unavailable. This can occur if a support program or file does not exist. This can also be used as a catch-all message when something you wanted to do doesn't work, but you don't know why.
        private const int EX_SOFTWARE    = 70; // An internal software error has been detected. This should be limited to non-operating system related errors if possible.
        

        private static async Task<int> Main(string[] args)
        {
            var localComand = new Command("local", "A local package")
            {
                new Argument<string>("file", "Package to validate.")
            };
            
            var remoteCommand = new Command("remote", "A package on a NuGet Feed")
            {
                new Argument<string>("packageId", "Package Id"),
                new Option<NuGetVersion?>(new[] { "--version", "-v" }, "Package version. Defaults to latest."),   
                new Option<Uri>(new []{"--feed-source", "-s"}, () => new Uri(NuGet.Configuration.NuGetConstants.V3FeedUrl), $"V3 NuGet Feed Source.")
            };

            var rootCommand = new RootCommand()
            {
                new Command("package", "Validates NuGet package health. Ensures your package meets the .NET Foundation's guidelines for secure packages.")
                {
                      localComand,
                      remoteCommand
                }
            };

            localComand.Handler = CommandHandler.Create<string>(RunLocalCommand);
            remoteCommand.Handler = CommandHandler.Create<string, NuGetVersion?, Uri>(RunRemoteCommand);

            return await rootCommand.InvokeAsync(args).ConfigureAwait(false);
        }

        private static async Task<int> RunLocalCommand(string file)
        {
            var directory = Directory.GetCurrentDirectory();

            if (Path.GetPathRoot(file) is { } root && !string.IsNullOrEmpty(root))
            {
                directory = root;
                file = Path.GetRelativePath(root, file);
            }

            var files = new Matcher(StringComparison.Ordinal)
                .AddInclude(file)
                .Execute(new DirectoryInfoWrapper(new DirectoryInfo(directory)));

            if (!files.HasMatches)
                return EXIT_FAILURE;

            using var cancellationTokenSource = new CancellationTokenSource();
            Console.CancelKeyPress += (_, eventArgs) =>
            {
                eventArgs.Cancel = !cancellationTokenSource.IsCancellationRequested;
                cancellationTokenSource.Cancel();
            };

            foreach (var actualFile in files.Files)
            {
                cancellationTokenSource.Token
                    .ThrowIfCancellationRequested();

                try
                {
                    var packageFile = new FileInfo(Path.Combine(directory, actualFile.Path));
                    if (packageFile.Exists)
                    {
                        await Console.Out.WriteLineAsync($"Validating {packageFile.FullName}").ConfigureAwait(false);
                    }

                    var isValid = await RunAsync(packageFile, cancellationTokenSource.Token).ConfigureAwait(false);

                    if (!isValid)
                        return EXIT_FAILURE;
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

            return EXIT_SUCCESS;
        }

        private static async Task<int> RunRemoteCommand(string packageId, NuGetVersion? version, Uri feedSource)
        {
            try
            {

                // null is getting passed in as 0.0.0 for some reason
                if(version != null && version.Major == 0 && version.Minor == 0 && version.Patch == 0)
                {
                    version = null;
                }

                using var cancellationTokenSource = new CancellationTokenSource();
                Console.CancelKeyPress += (_, eventArgs) =>
                {
                    eventArgs.Cancel = !cancellationTokenSource.IsCancellationRequested;
                    cancellationTokenSource.Cancel();
                };
                                
                using var downloader = new NuGetPackageDownloader(Console.Out);
                var packageFile = await downloader.DownloadAsync(packageId, version, feedSource, cancellationTokenSource.Token).ConfigureAwait(false);
                var versionString = version == null ? "" : version.ToFullString() + " ";
                await Console.Out.WriteLineAsync($"Validating {packageId} {versionString}from {packageFile.FullName}").ConfigureAwait(false);
                

                var isValid = await RunAsync(packageFile, cancellationTokenSource.Token).ConfigureAwait(false);

                return isValid ? EXIT_SUCCESS : EXIT_FAILURE;
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
            var compilerFlagsValid = result.CompilerFlagsResult is HasCompilerFlagsResult.Valid or HasCompilerFlagsResult.NothingToValidate;
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
                HasCompilerFlagsResult.Present => "⚠️ Present, not reproducible",
                HasCompilerFlagsResult.Valid => "✅ Valid",
                HasCompilerFlagsResult.Missing => "❌ Missing",
                HasCompilerFlagsResult.NothingToValidate => "✅ No files found to validate",
                _ => throw new ArgumentOutOfRangeException(nameof(result), result, $@"The value of argument '{nameof(result)}' ({result}) is invalid for enum type '{nameof(HasCompilerFlagsResult)}'.")
            };
        }
    }
}
