using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;

using Microsoft.Extensions.DependencyModel;

using NuGet.Protocol.Core.Types;

using NuGetPe.AssemblyMetadata;
using NuGetPe.Utility;

namespace NuGetPe
{
    public class SymbolValidator
    {
        private readonly IPackage _package;
        private readonly string _packagePath;
        private readonly IFolder _rootFolder;
        private readonly HttpClient _httpClient;

        public SymbolValidator(IPackage package, string packagePath, IFolder? rootFolder = null)
            : this(package, packagePath, rootFolder, httpClient: null)
        {
        }

        public SymbolValidator(IPackage package, string packagePath, IFolder? rootFolder, HttpClient? httpClient)
        {
            _package = package ?? throw new ArgumentNullException(nameof(package));
            _packagePath = packagePath ?? throw new ArgumentNullException(nameof(packagePath));
            _rootFolder = rootFolder ?? PathToTreeConverter.Convert(package.GetFiles().ToList());
            _httpClient = httpClient ?? new();

            if (httpClient == null)
            {
                UserAgent.SetUserAgent(_httpClient);
            }
        }

        public async Task<SymbolValidatorResult> Validate(CancellationToken cancellationToken = default)
        {
            try
            {
                // NuGet signs all its packages and stamps on the service index. Look for that.
                if (_package is ISignaturePackage sigPackage)
                {
                    await sigPackage.LoadSignatureDataAsync().ConfigureAwait(false);
                    if (sigPackage.RepositorySignature?.V3ServiceIndexUrl?.AbsoluteUri.Contains(".nuget.org/", StringComparison.OrdinalIgnoreCase) == true)
                    {
                        IsPublicPackage = true;
                    }
                }

                // Get relevant files to check
                var files = GetFilesToCheck();
                return await CalculateValidity(files, cancellationToken).ConfigureAwait(false);
            }
            catch (PlatformNotSupportedException e) when (!AppCompat.IsWindows)
            {
                // don't track PlatformNotSupportedException
                //DiagnosticsClient.TrackException(e, _package, IsPublicPackage);

                return new SymbolValidatorResult(
                    default, null,
                    default, null,
                    default, null,
                    exception: e
                );
            }
            catch(Exception e)
            {
                DiagnosticsClient.TrackException(e, _package, IsPublicPackage);

                var message = $"Validation Exception: {e.Message}";

                return new SymbolValidatorResult(
                    SymbolValidationResult.NoSymbols, message,
                    DeterministicResult.NonDeterministic, message,
                    HasCompilerFlagsResult.Missing, message
                );
            }
        }

        public IReadOnlyList<IFile> GetAllFiles() => GetFilesToCheck();

        private IReadOnlyList<IFile> GetFilesToCheck()
        {
            if (_package.PackageTypes.Contains(NuGet.Packaging.Core.PackageType.DotnetTool))
            {
                return GetToolFiles();
            }

            return GetLibraryFiles();
        }

        private IReadOnlyList<IFile> GetToolFiles()
        {

            var files = new List<IFile>();
            // For tool packages, we look in the tools folder for projects the user built
            // We will look for *.deps.json and read the libraries node for type: project.
            // Then we return the matching files in the same directory

            var depsFiles = _rootFolder["tools"]?.GetFiles().Where(f => f.Path.EndsWith(".deps.json", StringComparison.OrdinalIgnoreCase)) ?? Enumerable.Empty<IFile>();

            foreach(var depFile in depsFiles)
            {
                using var reader = new DependencyContextJsonReader();
                var context = reader.Read(depFile.GetStream());

                var runtimeLibs = context.RuntimeLibraries.Where(rl => "project".Equals(rl.Type, StringComparison.OrdinalIgnoreCase)).ToList();


                var userFiles = (from rl in runtimeLibs
                                join f in depFile.Parent!.GetFiles() on $"{rl.Name}.dll".ToUpperInvariant() equals f.Name.ToUpperInvariant()
                                select f).ToList();


                files.AddRange(userFiles);
            }

            return files;
        }

        private IReadOnlyList<IFile> GetLibraryFiles()
        {
            // For library packages, we look in lib and runtimes for files to check

            var libFiles = _rootFolder["lib"]?.GetFiles() ?? Enumerable.Empty<IFile>();
            var runtimeFiles = _rootFolder["runtimes"]?.GetFiles().Where(f => !IsNativeRuntimeFilePath(f.Path)) ?? Enumerable.Empty<IFile>();
            var files = libFiles.Union(runtimeFiles).ToList();


            static bool IsNativeRuntimeFilePath(string path)
                => path.Split('\\').Skip(2).FirstOrDefault() == "native";

            return files;
        }

        private async Task<SymbolValidatorResult> CalculateValidity(IReadOnlyList<IFile> files, CancellationToken cancellationToken)
        {
            var filesWithPdb = (from pf in files
                                let ext = Path.GetExtension(pf.Path)
                                where ".dll".Equals(ext, StringComparison.OrdinalIgnoreCase) ||
                                      ".exe".Equals(ext, StringComparison.OrdinalIgnoreCase) ||
                                      ".winmd".Equals(ext, StringComparison.OrdinalIgnoreCase)
                                select new FileWithPdb
                                {
                                    Primary = pf,
                                    Pdb = pf.GetAssociatedFiles().FirstOrDefault(af => ".pdb".Equals(Path.GetExtension(af.Path), StringComparison.OrdinalIgnoreCase))
                                })
                                .ToList();


            var sourceLinkErrors = new List<(IFile file, string errors)>();
            var noSourceLink = new List<IFile>();
            var noSymbols = new List<IFile>();
            var untrackedSources = new List<IFile>();
            var nonDeterministic = new List<IFile>();
            var nonReproducible = new List<IFile>();

            var allFilePaths = filesWithPdb.ToDictionary(pf => pf.Primary.Path);

            var pdbChecksumValid = true;

            foreach (var file in filesWithPdb.ToArray()) // work on array as we'll remove items that are satellite assemblies as we go
            {
                // Skip satellite assemblies
                if(IsSatelliteAssembly(file.Primary.Path))
                {
                    filesWithPdb.Remove(allFilePaths[file.Primary.Path]);
                    continue;
                }

                // If we have a PDB, try loading that first. If not, may be embedded.
                // Local checks first
                if(file.Pdb != null)
                {
                    if (! await ValidatePdb(file.Primary, file.Pdb.GetStream(),
                                            noSourceLink,
                                            sourceLinkErrors,
                                            untrackedSources,
                                            nonDeterministic,
                                            nonReproducible,
                                            false).ConfigureAwait(false))
                    {
                        pdbChecksumValid = false;
                        noSymbols.Add(file.Primary);
                    }
                }
                else // No PDB, see if it's embedded
                {
                    try
                    {

                        using var str = file.Primary.GetStream();

                        // Use descriptive file extension so files that appear in file system logging or that are
                        // leftover during an abrupt process termination can be debugged easier
                        var tempFileExtension = ".npe" + (string.IsNullOrEmpty(file.Primary.Extension) ? ".dat" : file.Primary.Extension);

                        using var tempFile = new TemporaryFile(str, tempFileExtension);

                        var assemblyMetadata = AssemblyMetadataReader.ReadMetaData(tempFile.FileName);

                        file.Primary.DebugData = assemblyMetadata?.DebugData;
                        if (assemblyMetadata?.DebugData.HasDebugInfo == true)
                        {
                            // we have an embedded pdb
                            if(!assemblyMetadata.DebugData.HasSourceLink)
                            {
                                noSourceLink.Add(file.Primary);
                            }

                            if (assemblyMetadata.DebugData.SourceLinkErrors.Count > 0)
                            {
                                // Has source link errors
                                sourceLinkErrors.Add((file.Primary, string.Join("\n", assemblyMetadata.DebugData.SourceLinkErrors)));
                            }

                            // Check for non-embedded sources
                            if (assemblyMetadata.DebugData.UntrackedSources.Count > 0 || !assemblyMetadata.DebugData.AllSourceLink)
                            {
                                untrackedSources.Add(file.Primary);
                            }

                            // Check for deterministic sources
                            if(!assemblyMetadata.DebugData.SourcesAreDeterministic)
                            {
                                nonDeterministic.Add(file.Primary);
                            }

                            // Check for reproducible build settings
                            if(!assemblyMetadata.DebugData.HasCompilerFlags || !assemblyMetadata.DebugData.CompilerVersionSupportsReproducible)
                            {
                                nonReproducible.Add(file.Primary);
                            }
                        }
                        else // no embedded pdb, try to look for it elsewhere
                        {
                            noSymbols.Add(file.Primary);
                        }

                    }
                    catch // an error occurred, no symbols
                    {
                        noSymbols.Add(file.Primary);
                    }
                }
            }


            var requireExternal = false;
            // See if any pdb's are missing and check for a snupkg on NuGet.org.
            if (noSymbols.Count > 0)
            {
                try
                {
                    // try to find a sibling snupkg file locally
                    var snupkgFilePath = Path.ChangeExtension(_packagePath, ".snupkg");
                    var symbolsFilePath = Path.ChangeExtension(_packagePath, ".symbols.nupkg");
                    if (File.Exists(snupkgFilePath))
                    {
                        await ReadSnupkgFile(snupkgFilePath).ConfigureAwait(false);
                    }
                    else if (File.Exists(symbolsFilePath))
                    {
                        await ReadSnupkgFile(symbolsFilePath).ConfigureAwait(false);
                    }
                    else if (IsPublicPackage)
                    {
                        // try to get on NuGet.org
                        // https://www.nuget.org/api/v2/symbolpackage/Newtonsoft.Json/12.0.3 -- Will redirect

#pragma warning disable CA2234 // Pass system uri objects instead of strings
#pragma warning disable CA1308 // Normalize strings to uppercase
                        using var response = await _httpClient.GetAsync($"https://globalcdn.nuget.org/symbol-packages/{_package.Id.ToLowerInvariant()}.{_package.Version.ToNormalizedString().ToLowerInvariant()}.snupkg", cancellationToken).ConfigureAwait(false);
#pragma warning restore CA1308 // Normalize strings to uppercase
#pragma warning restore CA2234 // Pass system uri objects instead of strings

                        if (response.IsSuccessStatusCode) // we'll get a 404 if none
                        {
#if NET5_0_OR_GREATER
                            using var getStream = await response.Content!.ReadAsStreamAsync(cancellationToken).ConfigureAwait(false);
#else
                            using var getStream = await response.Content!.ReadAsStreamAsync().ConfigureAwait(false);
#endif
                            using var tempFile = new TemporaryFile(getStream, ".npe.snupkg");
                            await ReadSnupkgFile(tempFile.FileName).ConfigureAwait(false);
                        }
                    }
                }
                catch // Could not check, leave status as-is
                {
                }

                async Task ReadSnupkgFile(string snupkgFilePath)
                {
                    requireExternal = true;

                    using var package = new ZipPackage(snupkgFilePath);

                    // Look for pdb's for the missing files
                    var dict = package.GetFiles().ToDictionary(k => k.Path);

                    foreach (var file in noSymbols.ToArray()) // from a copy so we can remove as we go
                    {
                        // file to look for
                        var pdbpath = Path.ChangeExtension(file.Path, ".pdb");

                        if (dict.TryGetValue(pdbpath, out var pdbfile))
                        {
                            // Validate
                            if (await ValidatePdb(file, pdbfile.GetStream(),
                                noSourceLink,
                                sourceLinkErrors,
                                untrackedSources,
                                nonDeterministic,
                                nonReproducible,
                                true).ConfigureAwait(false))
                            {
                                noSymbols.Remove(file);
                            }
                            else
                            {
                                pdbChecksumValid = false;
                            }
                        }
                    }
                }
            }

            // Check for Microsoft assemblies on the Microsoft symbol server
            if (noSymbols.Count > 0)
            {
                var microsoftFiles = noSymbols.Where(f => f.DebugData != null && IsMicrosoftFile(f)).ToList();

                foreach(var file in microsoftFiles)
                {
                    var pdbStream = await GetSymbolsAsync(file.DebugData!.SymbolKeys, cancellationToken).ConfigureAwait(false);
                    if(pdbStream != null)
                    {
                        requireExternal = true;

                        // Found a PDB for it
                        if(await ValidatePdb(file, pdbStream,
                            noSourceLink,
                            sourceLinkErrors,
                            untrackedSources,
                            nonDeterministic,
                            nonReproducible,
                            true).ConfigureAwait(false))
                        {
                            noSymbols.Remove(file);
                        }
                        else
                        {
                            pdbChecksumValid = false;
                        }
                    }
                }

            }

            var sourceLinkResult = SymbolValidationResult.NoSymbols;
            string? sourceLinkErrorMessage;

            DeterministicResult deterministicResult;
            string? deterministicErrorMessage;

            var compilerFlagsResult = HasCompilerFlagsResult.Valid;
            string? compilerFlagsMessage = null;

            if (noSymbols.Count == 0 && noSourceLink.Count == 0 && sourceLinkErrors.Count == 0)
            {
                if(untrackedSources.Count > 0)
                {
                    sourceLinkResult = SymbolValidationResult.HasUntrackedSources;

                    var sb = new StringBuilder("Contains untracked sources:\n");
                    sb.AppendLine("To Fix:");
                    sb.AppendLine("<EmbedUntrackedSources>true</EmbedUntrackedSources>");
                    sb.AppendLine("");
                    sb.AppendLine("Also, use 3.1.300 SDK to build or\nworkaround in: https://github.com/dotnet/sourcelink/issues/572");

                    foreach(var untracked in untrackedSources)
                    {
                        sb.AppendLine($"Assembly: {untracked.Path}");

                        foreach(var source in untracked.DebugData!.UntrackedSources)
                        {
                            sb.AppendLine($"  {source}");
                        }

                        sb.AppendLine();
                    }

                    sourceLinkErrorMessage = sb.ToString();
                }
                else if(filesWithPdb.Count == 0)
                {
                    sourceLinkResult = SymbolValidationResult.NothingToValidate;
                    sourceLinkErrorMessage = "No files found to validate";
                }
                else if(requireExternal)
                {
                    sourceLinkResult = SymbolValidationResult.ValidExternal;
                    sourceLinkErrorMessage = null;
                }
                else
                {
                    sourceLinkResult = SymbolValidationResult.Valid;
                    sourceLinkErrorMessage = null;
                }
            }
            else
            {
                var found = false;
                var sb = new StringBuilder();
                if (noSourceLink.Count > 0)
                {
                    sourceLinkResult = SymbolValidationResult.NoSourceLink;

                    sb.AppendLine($"Missing Source Link for:\n{string.Join("\n", noSourceLink.Select(p => p.Path)) }");
                    found = true;
                }

                if(sourceLinkErrors.Count > 0)
                {
                    sourceLinkResult = SymbolValidationResult.InvalidSourceLink;

                    if (found)
                        sb.AppendLine();

                    foreach(var (file, errors) in sourceLinkErrors)
                    {
                        sb.AppendLine($"Source Link errors for {file.Path}:\n{string.Join("\n", errors) }");
                    }

                    found = true;
                }

                if (noSymbols.Count > 0) // No symbols "wins" as it's more severe
                {
                    sourceLinkResult = SymbolValidationResult.NoSymbols;

                    if (found)
                        sb.AppendLine();

                    if(!pdbChecksumValid)
                    {
                        sb.AppendLine("Some PDB's checksums do not match their PE files and are shown as missing.");
                    }

                    sb.AppendLine($"Missing Symbols for:\n{string.Join("\n", noSymbols.Select(p => p.Path)) }");
                    found = true;
                }
                else if(!found)
                {
                    throw new InvalidOperationException("This branch of code should never be reached because either one of {noSymbols.Count, noSourceLink.Count, sourceLinkErrors.Count} must be > 0.");
                }

                sourceLinkErrorMessage = sb.ToString();
            }

            if(sourceLinkResult == SymbolValidationResult.NothingToValidate)
            {
                deterministicResult = DeterministicResult.NothingToValidate;
                deterministicErrorMessage = null;

                compilerFlagsResult = HasCompilerFlagsResult.NothingToValidate;
                compilerFlagsMessage = null;
            }
            else if(sourceLinkResult == SymbolValidationResult.NoSymbols)
            {
                deterministicResult = DeterministicResult.NonDeterministic;
                deterministicErrorMessage = "Missing Symbols";

                compilerFlagsResult = HasCompilerFlagsResult.Missing;
                compilerFlagsMessage = "Missing Symbols";
            }
            else if(nonDeterministic.Count > 0)
            {
                deterministicResult = DeterministicResult.NonDeterministic;

                var sb = new StringBuilder();
                sb.AppendLine("Ensure that the following property is enabled for CI builds\nand you're using at least the 2.1.300 SDK:");
                sb.AppendLine();
                sb.AppendLine("<ContinuousIntegrationBuild>true</ContinuousIntegrationBuild>");
                sb.AppendLine();
                sb.AppendLine("The following assemblies have not been compiled with deterministic settings:");

                foreach(var file in nonDeterministic)
                {
                    sb.AppendLine(file.Path);
                }

                deterministicErrorMessage = sb.ToString();

            }
            else if(sourceLinkResult == SymbolValidationResult.HasUntrackedSources)
            {
                deterministicResult = DeterministicResult.HasUntrackedSources;
                deterministicErrorMessage = null;
            }
            else
            {
                deterministicResult = DeterministicResult.Valid;
                deterministicErrorMessage = null;
            }

            if (nonReproducible.Count > 0)
            {
                // See if they're here because they're missing or just too old
                var first = nonReproducible.First();
                compilerFlagsResult = (first.DebugData?.HasCompilerFlags == true &&
                                       first.DebugData?.CompilerVersionSupportsReproducible == false) ? HasCompilerFlagsResult.Present : HasCompilerFlagsResult.Missing;

                var sb = new StringBuilder();
                sb.AppendLine("Ensure you're using at least the 5.0.300 SDK or MSBuild 16.10:");

                if(sourceLinkResult == SymbolValidationResult.NoSymbols)
                {
                    sb.AppendLine("Assemblies must have symbols:");
                }
                else
                {
                    sb.AppendLine("The following assemblies have not been compiled with a new enough compiler:");
                }

                foreach (var file in nonReproducible)
                {
                    sb.AppendLine(file.Path);
                }

                compilerFlagsMessage = sb.ToString();
            }

            return new SymbolValidatorResult(sourceLinkResult, sourceLinkErrorMessage,
                deterministicResult, deterministicErrorMessage,
                compilerFlagsResult, compilerFlagsMessage);
        }

        private static bool IsMicrosoftFile(IFile file)
        {
            using var stream = StreamUtility.MakeSeekable(file.GetStream(), disposeOriginal: true);
            var peFile = new PeNet.PeFile(stream);

            var subject = AppCompat.IsSupported(RuntimeFeature.Cryptography)
                ? peFile.Authenticode?.SigningCertificate?.Subject
                : CryptoUtility.GetSigningCertificate(peFile)?.Subject.ToString();

            return subject?.EndsWith(", O=Microsoft Corporation, L=Redmond, S=Washington, C=US", StringComparison.OrdinalIgnoreCase) == true;
        }

        private static async Task<bool> ValidatePdb(IFile input,
            Stream pdbStream,
            List<IFile> noSourceLink,
            List<(IFile file, string errors)> sourceLinkErrors,
            List<IFile> untrackedSources,
            List<IFile> nonDeterministic,
            List<IFile> nonReproducible,
            bool validateChecksum)
        {
            var peStream = StreamUtility.MakeSeekable(input.GetStream(), true);
            try
            {
                // TODO: Verify that the PDB and DLL match

                // This might throw an exception because we don't know if it's a full PDB or portable
                // Try anyway in case it succeeds as a ppdb
                try
                {
                    if(input.DebugData == null || !input.DebugData.HasDebugInfo) // get it again if this is a shell with keys
                    {
                        using var stream = StreamUtility.MakeSeekable(pdbStream, true);
                        input.DebugData = await AssemblyMetadataReader.ReadDebugData(peStream, stream).ConfigureAwait(false);
                    }

                    // Check to see if the PDB is valid, but only for pdb's that aren't alongside the PE file
                    if(validateChecksum && !input.DebugData.PdbChecksumIsValid)
                    {
                        return false;
                    }

                    if (!input.DebugData.HasSourceLink)
                    {
                        // Have a PDB, but it's missing source link data
                        noSourceLink.Add(input);
                    }

                    if (input.DebugData.SourceLinkErrors.Count > 0)
                    {
                        // Has source link errors
                        sourceLinkErrors.Add((input, string.Join("\n", input.DebugData.SourceLinkErrors)));
                    }

                    // Check for non-embedded sources
                    if(input.DebugData.UntrackedSources.Count > 0 || !input.DebugData.AllSourceLink)
                    {
                        untrackedSources.Add(input);
                    }

                    // Check for deterministic sources
                    if (!input.DebugData.SourcesAreDeterministic)
                    {
                        nonDeterministic.Add(input);
                    }

                    if(!input.DebugData.HasCompilerFlags || !input.DebugData.CompilerVersionSupportsReproducible)
                    {
                        nonReproducible.Add(input);
                    }

                }
                catch (ArgumentNullException)
                {
                    // Have a PDB, but there's an error with the source link data
                    noSourceLink.Add(input);
                }
            }
            finally
            {
                await peStream.DisposeAsync().ConfigureAwait(false);
            }

            return true;
        }


        // From https://github.com/ctaggart/SourceLink/blob/51e5b47ae64d87447a0803cec559947242fe935b/dotnet-sourcelink/Program.cs
        private static bool IsSatelliteAssembly(string path)
        {
            var match = Regex.Match(path, @"^(.*)\\[^\\]+\\([^\\]+).resources.dll$");

            return match.Success;

            // Satellite assemblies may not be in the same package as their main dll
           // return match.Success && dlls.Contains($"{match.Groups[1]}\\{match.Groups[2]}.dll");
        }

        //private static readonly string? apiLocation = "http://localhost:7071/api/MsdlProxy";
        private static readonly string? ApiLocation = Environment.GetEnvironmentVariable("MSDL_PROXY_LOCATION");

        private async Task<Stream?> GetSymbolsAsync(IReadOnlyList<SymbolKey> symbolKeys, CancellationToken cancellationToken = default)
        {            
            foreach (var symbolKey in symbolKeys)
            {

                Uri uri;
                if(AppCompat.IsWasm && !string.IsNullOrWhiteSpace(ApiLocation))
                {                    
                    uri = new Uri($"{ApiLocation}?symbolKey={symbolKey.Key}", UriKind.RelativeOrAbsolute);                                        
                }
                else
                {
                    uri = new Uri(new Uri("https://msdl.microsoft.com/download/symbols/"), symbolKey.Key);
                }

                using var request = new HttpRequestMessage
                {
                    Method = HttpMethod.Get,
                    RequestUri = uri
                };

                if (symbolKey.Checksums?.Any() == true)
                {
                    request.Headers.Add("SymbolChecksum", string.Join(";", symbolKey.Checksums));
                }

                using var response = await _httpClient.SendAsync(request, cancellationToken).ConfigureAwait(false);
                if (!response.IsSuccessStatusCode)
                {
                    continue;
                }

                var pdbStream = new MemoryStream();
#if NET5_0_OR_GREATER
                await response.Content!.CopyToAsync(pdbStream, cancellationToken).ConfigureAwait(false);
#else
                await response.Content!.CopyToAsync(pdbStream).ConfigureAwait(false);
#endif
                pdbStream.Position = 0;

                return pdbStream;
            }

            return null;
        }

        /// <summary>
        /// Package is available from a public feed
        /// </summary>
        public bool IsPublicPackage { get; private set; }

        private class FileWithPdb
        {
#pragma warning disable CS8618 // Non-nullable field is uninitialized. Consider declaring as nullable.
            public IFile Primary { get; set; }
#pragma warning restore CS8618 // Non-nullable field is uninitialized. Consider declaring as nullable.
            public IFile? Pdb { get; set; }
        }
    }
}
