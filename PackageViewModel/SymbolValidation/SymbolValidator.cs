using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using AuthenticodeExaminer;
using Microsoft.Extensions.DependencyModel;
using NuGet.Packaging;
using NuGetPe;
using NuGetPe.AssemblyMetadata;
using PackageExplorerViewModel.Utilities;

namespace PackageExplorerViewModel
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
        /// In progress
        /// </summary>
        Pending,

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
        /// In Progress
        /// </summary>
        Pending,
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
        /// Symbols have compiler flag data
        /// </summary>
        Present,

        /// <summary>
        /// In Progress
        /// </summary>
        Pending,
        /// <summary>
        /// Symbols do not have compiler flag data
        /// </summary>
        Missing,
        /// <summary>
        /// No relevant files to validate. 
        /// </summary>
        NothingToValidate
    }

    public class SymbolValidator : INotifyPropertyChanged
    {
        private readonly IPackage _package;
        private readonly HttpClient _httpClient = new();
        private readonly PackageFolder _rootFolder;
        private readonly string _packagePath;


        public SymbolValidator(IPackage package, string packagePath, PackageFolder? rootFolder)
        {
            _package = package ?? throw new ArgumentNullException(nameof(package));
            _packagePath = packagePath ?? throw new ArgumentNullException(nameof(packagePath));

            // NuGet signs all its packages and stamps on the service index. Look for that.
            if (package is ISignaturePackage sigPackage)
            {
                if (sigPackage.RepositorySignature?.V3ServiceIndexUrl?.AbsoluteUri.Contains(".nuget.org/", StringComparison.OrdinalIgnoreCase) == true)
                {
                    IsPublicPackage = true;
                }
            }

            _rootFolder = rootFolder ?? PathToTreeConverter.Convert(package.GetFiles().ToList(), null);


            SourceLinkResult = SymbolValidationResult.Pending;
            DeterministicResult = DeterministicResult.Pending;
            CompilerFlagsResult = HasCompilerFlagsResult.Pending;               
        }  

        public async Task ResetToDefault()
        {
            SourceLinkResult = SymbolValidationResult.Pending;
            SourceLinkErrorMessage = null;
            DeterministicResult = DeterministicResult.Pending;
            DeterministicErrorMessage = null;
            CompilerFlagsResult = HasCompilerFlagsResult.Pending;
            HasCompilerFlagsMessage = null;

            await Validate().ConfigureAwait(false);
        }

        public event PropertyChangedEventHandler? PropertyChanged;

        public async Task Validate()
        {
            try
            {
                // Get relevant files to check
                var files = GetFilesToCheck();

                await Task.Run(async () => await CalculateValidity(files!).ConfigureAwait(false));
            }
            catch(Exception e)
            {
                DiagnosticsClient.TrackException(e, _package, IsPublicPackage);
            }
            finally
            {
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(SourceLinkResult)));
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(SourceLinkErrorMessage)));
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(DeterministicResult)));
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(DeterministicErrorMessage)));
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(CompilerFlagsResult)));
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(HasCompilerFlagsMessage)));
            }

        }

        private IReadOnlyList<PackageFile> GetFilesToCheck()
        {
            if(_package.PackageTypes.Any(pt => "DotnetTool".Equals(pt.Name, StringComparison.OrdinalIgnoreCase)))
            {
                return GetToolFiles();
            }

            return GetLibraryFiles();
        }

        private IReadOnlyList<PackageFile> GetToolFiles()
        {

            var files = new List<PackageFile>();
            // For tool packages, we look in the tools folder for projects the user built
            // We will look for *.deps.json and read the libraries node for type: project.
            // Then we return the matching files in the same directory

            var depsFiles = _rootFolder["tools"]?.GetFiles().Where(f => f.Path.EndsWith(".deps.json", StringComparison.OrdinalIgnoreCase)) ?? Enumerable.Empty<IPackageFile>();

            foreach(PackageFile depFile in depsFiles)
            {
                using var reader = new DependencyContextJsonReader();
                var context = reader.Read(depFile.GetStream());

                var runtimeLibs = context.RuntimeLibraries.Where(rl => "project".Equals(rl.Type, StringComparison.OrdinalIgnoreCase)).ToList();


                var userFiles = (from rl in runtimeLibs
                                join f in depFile.Parent!.GetFiles().Where(pf => pf is PackageFile).Cast<PackageFile>() on $"{rl.Name}.dll".ToUpperInvariant() equals Path.GetFileName(f.Path).ToUpperInvariant()
                                select f).ToList();


                files.AddRange(userFiles);
            }

            return files;            
        }

        private IReadOnlyList<PackageFile> GetLibraryFiles()
        {
            // For library packages, we look in lib and runtimes for files to check

            var libFiles = _rootFolder["lib"]?.GetFiles() ?? Enumerable.Empty<IPackageFile>();
            var runtimeFiles = _rootFolder["runtimes"]?.GetFiles().Where(f => !IsNativeRuntimeFilePath(f.Path)) ?? Enumerable.Empty<IPackageFile>();
            var files = libFiles.Union(runtimeFiles).Where(pf => pf is PackageFile).Cast<PackageFile>().ToList();


            static bool IsNativeRuntimeFilePath(string path)
                => path.Split('\\').Skip(2).FirstOrDefault() == "native";

            return files!;
        }

        private async Task CalculateValidity(IReadOnlyList<PackageFile> files)
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


            var sourceLinkErrors = new List<(PackageFile file, string errors)>();
            var noSourceLink = new List<PackageFile>();
            var noSymbols = new List<FileWithDebugData>();
            var untrackedSources = new List<FileWithDebugData>();
            var nonDeterministic = new List<PackageFile>();
            var nonReproducible = new List<PackageFile>();

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
                    var filePair = new FileWithDebugData(file.Primary, null);
                    if (! await ValidatePdb(filePair, file.Pdb.GetStream(),
                                            noSourceLink,
                                            sourceLinkErrors,
                                            untrackedSources,
                                            nonDeterministic,
                                            nonReproducible,
                                            false).ConfigureAwait(false))
                    {
                        pdbChecksumValid = false;
                        noSymbols.Add(filePair);
                    }
                }
                else // No PDB, see if it's embedded
                {
                    try
                    {

                        using var str = file.Primary.GetStream();
                        using var tempFile = new TemporaryFile(str);

                        var assemblyMetadata = AssemblyMetadataReader.ReadMetaData(tempFile.FileName);

                        if(assemblyMetadata?.DebugData.HasDebugInfo == true)
                        {
                            file.Primary.DebugData = assemblyMetadata.DebugData;

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
                                var filePair = new FileWithDebugData(file.Primary, assemblyMetadata.DebugData);
                                untrackedSources.Add(filePair);
                            }

                            // Check for deterministic sources
                            if(!assemblyMetadata.DebugData.SourcesAreDeterministic)
                            {
                                nonDeterministic.Add(file.Primary);
                            }

                            // Check for reproducible build settings
                            if(!assemblyMetadata.DebugData.HasCompilerFlags)
                            {
                                nonReproducible.Add(file.Primary);
                            }
                        }
                        else // no embedded pdb, try to look for it elsewhere
                        {
                            noSymbols.Add(new FileWithDebugData(file.Primary, assemblyMetadata?.DebugData));
                        }

                    }
                    catch // an error occured, no symbols
                    {
                        noSymbols.Add(new FileWithDebugData(file.Primary, null));
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
                        var response = await _httpClient.GetAsync($"https://www.nuget.org/api/v2/symbolpackage/{_package.Id}/{_package.Version.ToNormalizedString()}").ConfigureAwait(false);
#pragma warning restore CA2234 // Pass system uri objects instead of strings

                        if (response.IsSuccessStatusCode) // we'll get a 404 if none
                        {
                            using var getStream = await response.Content!.ReadAsStreamAsync();
                            using var tempFile = new TemporaryFile(getStream, ".snupkg");
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
                        var pdbpath = Path.ChangeExtension(file.File.Path, ".pdb");

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
                var microsoftFiles = noSymbols.Where(f => f.DebugData != null && IsMicrosoftFile(f.File)).ToList();

                foreach(var file in microsoftFiles)
                {
                    var pdbStream = await GetSymbolsAsync(file.DebugData!.SymbolKeys);
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

            // Clear out result status
            CompilerFlagsResult = HasCompilerFlagsResult.Present;
            HasCompilerFlagsMessage = null;


            if (noSymbols.Count == 0 && noSourceLink.Count == 0 && sourceLinkErrors.Count == 0)
            {
                if(untrackedSources.Count > 0)
                {
                    SourceLinkResult = SymbolValidationResult.HasUntrackedSources;

                    var sb = new StringBuilder("Contains untracked sources:\n");
                    sb.AppendLine("To Fix:");
                    sb.AppendLine("<EmbedUntrackedSources>true</EmbedUntrackedSources>");
                    sb.AppendLine("");
                    sb.AppendLine("Also, use 3.1.300 SDK to build or\nworkaround in: https://github.com/dotnet/sourcelink/issues/572");

                    foreach(var untracked in untrackedSources)
                    {
                        sb.AppendLine($"Assembly: {untracked.File.Path}");

                        foreach(var source in untracked.DebugData!.UntrackedSources)
                        {
                            sb.AppendLine($"  {source}");
                        }

                        sb.AppendLine();
                    }

                    SourceLinkErrorMessage = sb.ToString();
                }
                else if(filesWithPdb.Count == 0)
                {
                    SourceLinkResult = SymbolValidationResult.NothingToValidate;
                    SourceLinkErrorMessage = "No files found to validate";
                }
                else if(requireExternal)
                {
                    SourceLinkResult = SymbolValidationResult.ValidExternal;
                    SourceLinkErrorMessage = null;
                }
                else
                {
                    SourceLinkResult = SymbolValidationResult.Valid;
                    SourceLinkErrorMessage = null;
                }
            }
            else
            {
                var found = false;
                var sb = new StringBuilder();
                if (noSourceLink.Count > 0)
                {                    
                    SourceLinkResult = SymbolValidationResult.NoSourceLink;

                    sb.AppendLine($"Missing Source Link for:\n{string.Join("\n", noSourceLink.Select(p => p.Path)) }");
                    found = true;
                }

                if(sourceLinkErrors.Count > 0)
                {
                    SourceLinkResult = SymbolValidationResult.InvalidSourceLink;

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
                    SourceLinkResult = SymbolValidationResult.NoSymbols;

                    if (found)
                        sb.AppendLine();

                    if(!pdbChecksumValid)
                    {
                        sb.AppendLine("Some PDB's checksums do not match their PE files and are shown as missing.");
                    }

                    sb.AppendLine($"Missing Symbols for:\n{string.Join("\n", noSymbols.Select(p => p.File.Path)) }");
                }

                SourceLinkErrorMessage = sb.ToString();
            }

            if(SourceLinkResult == SymbolValidationResult.NothingToValidate)
            {
                DeterministicResult = DeterministicResult.NothingToValidate;
                DeterministicErrorMessage = null;

                CompilerFlagsResult = HasCompilerFlagsResult.NothingToValidate;
                HasCompilerFlagsMessage = null;
            }
            else if(SourceLinkResult == SymbolValidationResult.NoSymbols)
            {
                DeterministicResult = DeterministicResult.NonDeterministic;
                DeterministicErrorMessage = "Missing Symbols";

                CompilerFlagsResult = HasCompilerFlagsResult.Missing;
                HasCompilerFlagsMessage = "Missing Symbols";
            }
            else if(nonDeterministic.Count > 0)
            {
                DeterministicResult = DeterministicResult.NonDeterministic;

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

                DeterministicErrorMessage = sb.ToString();

            }
            else if(SourceLinkResult == SymbolValidationResult.HasUntrackedSources)
            {
                DeterministicResult = DeterministicResult.HasUntrackedSources;
                DeterministicErrorMessage = null;
            }
            else
            {
                DeterministicResult = DeterministicResult.Valid;
                DeterministicErrorMessage = null;
            }

            if (nonReproducible.Count > 0)
            {
                CompilerFlagsResult = HasCompilerFlagsResult.Missing;

                var sb = new StringBuilder();
                sb.AppendLine("Ensure you're using at least the 3.1.400 SDK or MSBuild 16.7:");

                if(SourceLinkResult == SymbolValidationResult.NoSymbols)
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

                HasCompilerFlagsMessage = sb.ToString();
            }
        }

        private static bool IsMicrosoftFile(PackageFile file)
        {
            IReadOnlyList<AuthenticodeSignature> sigs;
            SignatureCheckResult isValidSig;
            using (var str = file.GetStream())
            using (var tempFile = new TemporaryFile(str, Path.GetExtension(file.Name)))
            {
                var extractor = new FileInspector(tempFile.FileName);

                sigs = extractor.GetSignatures().ToList();
                isValidSig = extractor.Validate();

                if(isValidSig == SignatureCheckResult.Valid && sigs.Count > 0)
                {
                    return sigs[0].SigningCertificate.Subject.EndsWith(", O=Microsoft Corporation, L=Redmond, S=Washington, C=US", StringComparison.OrdinalIgnoreCase);
                }
            }

            return false;
        }

        private static async Task<bool> ValidatePdb(FileWithDebugData input,
                                        Stream pdbStream,
                                        List<PackageFile> noSourceLink,
                                        List<(PackageFile file, string errors)> sourceLinkErrors,
                                        List<FileWithDebugData> untrackedSources,
                                        List<PackageFile> nonDeterministic,
                                        List<PackageFile> nonReproducible,
                                        bool validateChecksum)
        {
            var peStream = MakeSeekable(input.File.GetStream(), true);
            try
            {
                // TODO: Verify that the PDB and DLL match

                // This might throw an exception because we don't know if it's a full PDB or portable
                // Try anyway in case it succeeds as a ppdb
                try
                {
                    if(input.DebugData == null || !input.DebugData.HasDebugInfo) // get it again if this is a shell with keys
                    {
                        using var stream = MakeSeekable(pdbStream, true);
                        input.DebugData = await AssemblyMetadataReader.ReadDebugData(peStream, stream).ConfigureAwait(false);
                    }

                    // Check to see if the PDB is valid, but only for pdb's that aren't alongside the PE file                    
                    if(validateChecksum && !input.DebugData.PdbChecksumIsValid)
                    {
                        return false;
                    }

                    // Store in the PackageFile
                    input.File.DebugData = input.DebugData;

                    if (!input.DebugData.HasSourceLink)
                    {
                        // Have a PDB, but it's missing source link data
                        noSourceLink.Add(input.File);
                    }

                    if (input.DebugData.SourceLinkErrors.Count > 0)
                    {
                        // Has source link errors
                        sourceLinkErrors.Add((input.File, string.Join("\n", input.DebugData.SourceLinkErrors)));
                    }

                    // Check for non-embedded sources
                    if(input.DebugData.UntrackedSources.Count > 0 || !input.DebugData.AllSourceLink)
                    {
                        untrackedSources.Add(input);
                    }

                    // Check for deterministic sources
                    if (!input.DebugData.SourcesAreDeterministic)
                    {
                        nonDeterministic.Add(input.File);
                    }

                    if(!input.DebugData.HasCompilerFlags)
                    {
                        nonReproducible.Add(input.File);
                    }

                }
                catch (ArgumentNullException)
                {
                    // Have a PDB, but there's an error with the source link data
                    noSourceLink.Add(input.File);
                }
            }
            finally
            {
                peStream.Dispose();
            }

            return true;
        }

        private static Stream MakeSeekable(Stream stream, bool disposeOriginal = false)
        {
            if (stream.CanSeek)
            {
                return stream;
            }

            var memoryStream = new MemoryStream();
            stream.CopyTo(memoryStream);
            memoryStream.Position = 0;

            if (disposeOriginal)
            {
                stream.Dispose();
            }
            return memoryStream;
        }


        // From https://github.com/ctaggart/SourceLink/blob/51e5b47ae64d87447a0803cec559947242fe935b/dotnet-sourcelink/Program.cs
        private static bool IsSatelliteAssembly(string path)
        {
            var match = Regex.Match(path, @"^(.*)\\[^\\]+\\([^\\]+).resources.dll$");

            return match.Success;

            // Satellite assemblies may not be in the same package as their main dll
           // return match.Success && dlls.Contains($"{match.Groups[1]}\\{match.Groups[2]}.dll");
        }

        private async Task<Stream?> GetSymbolsAsync(IReadOnlyList<SymbolKey> symbolKeys, CancellationToken cancellationToken = default)
        {
            foreach (var symbolKey in symbolKeys)
            {
               //var uri = new Uri(new Uri("https://symbols.nuget.org/download/symbols/"), symbolKey.Key);
               var uri = new Uri(new Uri("https://msdl.microsoft.com/download/symbols/"), symbolKey.Key);

                using var request = new HttpRequestMessage
                {
                    Method = HttpMethod.Get,
                    RequestUri = uri
                };

                if (symbolKey.Checksums?.Any() == true)
                {
                    request.Headers.Add("SymbolChecksum", string.Join(";", symbolKey.Checksums));
                }

                using var response = await _httpClient.SendAsync(request, cancellationToken);
                if (!response.IsSuccessStatusCode)
                {
                    continue;
                }

                var pdbStream = new MemoryStream();
                await response.Content!.CopyToAsync(pdbStream, cancellationToken);
                pdbStream.Position = 0;

                return pdbStream;
            }

            return null;
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

        public HasCompilerFlagsResult CompilerFlagsResult { get; private set; }
        public string? HasCompilerFlagsMessage { get; private set; }

        public string? DeterministicErrorMessage
        {
            get; private set;
        }

        /// <summary>
        /// Package is available from a public feed
        /// </summary>
        public bool IsPublicPackage { get; }

        private class FileWithPdb
        {
#pragma warning disable CS8618 // Non-nullable field is uninitialized. Consider declaring as nullable.
            public PackageFile Primary { get; set; }
#pragma warning restore CS8618 // Non-nullable field is uninitialized. Consider declaring as nullable.
            public PackageFile? Pdb { get; set; }
        }

        private class FileWithDebugData
        {
            public FileWithDebugData(PackageFile file, AssemblyDebugData? debugData)
            {
                File = file;
                DebugData = debugData;
            }

            public PackageFile File { get; }
            public AssemblyDebugData? DebugData { get; set; }
        }
    }
}
