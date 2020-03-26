using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

using NuGet.Packaging;

using NuGetPackageExplorer.Types;

using NuGetPe;
using NuGetPe.AssemblyMetadata;

namespace PackageExplorerViewModel
{
    public enum SymbolValidationResult
    {
        Valid,
        ValidExternal,
        NoSourceLink,
        NoSymbols,
        Pending,
        NothingToValidate
    }

    public class SymbolValidator : INotifyPropertyChanged
    {
        private readonly PackageViewModel _packageViewModel;
        private readonly IPackage _package;
        private readonly bool _publishedOnNuGetOrg;

        public SymbolValidator(PackageViewModel packageViewModel, IPackage package)
        {
            _packageViewModel = packageViewModel ?? throw new ArgumentNullException(nameof(packageViewModel));
            _package = package;
            _packageViewModel.PropertyChanged += _packageViewModel_PropertyChanged;

            Result = SymbolValidationResult.Pending;

            // NuGet signs all its packages and stamps on the service index. Look for that.
            if(package is ISignaturePackage sigPackage)
            {
                if (sigPackage.RepositorySignature?.V3ServiceIndexUrl?.AbsoluteUri.Contains(".nuget.org/", StringComparison.OrdinalIgnoreCase) == true)
                {
                    _publishedOnNuGetOrg = true;
                }
            }            
        }

        private void _packageViewModel_PropertyChanged(object? sender, PropertyChangedEventArgs e)
        {
            if(e.PropertyName == null)
            {
                Result = SymbolValidationResult.Pending;
                ErrorMessage = null;
                Refresh();
            }
        }

        public event PropertyChangedEventHandler? PropertyChanged;

        public async void Refresh()
        {
            try
            {
                // Get relevant files to check
                var libFiles = _packageViewModel.RootFolder["lib"]?.GetFiles() ?? Enumerable.Empty<IPackageFile>();
                var runtimeFiles = _packageViewModel.RootFolder["runtimes"]?.GetFiles() ?? Enumerable.Empty<IPackageFile>();
                var files = libFiles.Union(runtimeFiles).Where(pf => pf is PackageFile).Cast<PackageFile>().ToList();

                await Task.Run(async () => await CalculateValidity(files));
            }
            catch(Exception e)
            {
                DiagnosticsClient.TrackException(e);
            }
            finally
            {
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(Result)));
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(ErrorMessage)));
            }
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
                     

            var noSourceLink = new List<PackageFile>();
            var noSymbols = new List<PackageFile>();

            foreach(var file in filesWithPdb)
            {
                // If we have a PDB, try loading that first. If not, may be embedded. Ottherwise, missing for now
                // TODO: Check for symbol server and SNUPKG later

                if(file.Pdb != null)
                {
                    var peStream = MakeSeekable(file.Primary.GetStream(), true);
                    try
                    {

                        // This might throw an exception because we don't know if it's a full PDB or portable
                        // Try anyway in case it succeeds as a ppdb
                        try
                        {
                            using (var stream = MakeSeekable(file.Pdb.GetStream(), true))
                            {
                                var data = AssemblyMetadataReader.ReadDebugData(peStream, stream);

                                if(!data.HasSourceLink)
                                {
                                    // Have a PDB, but it's missing source link data
                                    noSourceLink.Add(file.Primary);
                                }
                            }

                        }
                        catch (ArgumentNullException)
                        {
                            // Have a PDB, but ithere's an error with the source link data
                            noSourceLink.Add(file.Primary);
                        }
                    }
                    finally
                    {
                        peStream?.Dispose();
                    }
                }
                else // No PDB, see if it's embedded
                {
                    var tempFile = Path.GetTempFileName();
                    try
                    {                        
                        using (var str = file.Primary.GetStream())
                        using (var fileStream = File.OpenWrite(tempFile))
                        {
                            str.CopyTo(fileStream);
                        }

                        var assemblyMetadata = AssemblyMetadataReader.ReadMetaData(tempFile);

                        if(assemblyMetadata?.DebugData != null)
                        {
                            // we have an embedded pdb
                            if(!assemblyMetadata.DebugData.HasSourceLink)
                            {
                                noSourceLink.Add(file.Primary);
                            }
                        }
                        else // no embedded pdb, try to look for it
                        {
                            noSymbols.Add(file.Primary);
                        }

                    }
                    catch // an error occured, no symbols
                    {
                        noSymbols.Add(file.Primary);
                    }
                    finally
                    {
                        if (File.Exists(tempFile))
                        {
                            try
                            {
                                File.Delete(tempFile);
                            }
                            catch
                            {
                            }
                        }
                    }
                }
            }


            var requireExternal = false;
            // See if any pdb's are missing and check for a snupkg on NuGet.org. 
            if (noSymbols.Count > 0 && _publishedOnNuGetOrg)
            {
                // try to get on NuGet.org
                // https://www.nuget.org/api/v2/symbolpackage/Newtonsoft.Json/12.0.3 -- Will redirect
                using var client = new HttpClient();

                var tempFile = Path.GetTempFileName();
                try
                {
#pragma warning disable CA2234 // Pass system uri objects instead of strings
                    var response = await client.GetAsync($"https://www.nuget.org/api/v2/symbolpackage/{_package.Id}/{_package.Version.ToNormalizedString()}").ConfigureAwait(false);
#pragma warning restore CA2234 // Pass system uri objects instead of strings

                    if (response.IsSuccessStatusCode) // we'll get a 404 if none
                    {
                        requireExternal = true;

                        using (var getStream = await response.Content.ReadAsStreamAsync())
                        using (var fileStream = File.OpenWrite(tempFile))
                        {
                            await getStream.CopyToAsync(fileStream);
                        }

                        using var package = new ZipPackage(tempFile);

                        // Look for pdb's for the missing files
                        var dict = package.GetFiles().ToDictionary(k => k.Path);

                        foreach(var file in noSymbols.ToArray()) // from a copy so we can remove as we go
                        {
                            // file to look for

                            var ext = Path.GetExtension(file.Path);
                            var pdbpath = $"{file.Path[0..^ext.Length]}.pdb";

                            if(dict.TryGetValue(pdbpath, out var pdbfile))
                            {
                                noSymbols.Remove(file);

                                // Validate
                                var peStream = MakeSeekable(file.GetStream(), true);
                                try
                                {
                                    // This might throw an exception because we don't know if it's a full PDB or portable
                                    // Try anyway in case it succeeds as a ppdb
                                    try
                                    {
                                        using (var stream = MakeSeekable(pdbfile.GetStream(), true))
                                        {
                                            var data = AssemblyMetadataReader.ReadDebugData(peStream, stream);

                                            if (!data.HasSourceLink)
                                            {
                                                // Have a PDB, but it's missing source link data
                                                noSourceLink.Add(file);
                                            }
                                        }

                                    }
                                    catch (ArgumentNullException)
                                    {
                                        // Have a PDB, but ithere's an error with the source link data
                                        noSourceLink.Add(file);
                                    }
                                }
                                finally
                                {
                                    peStream?.Dispose();
                                }
                            }
                        }
                    }                    
                }
                catch // Could not check, leave status as-is
                {
                }
                finally
                {
                    if (File.Exists(tempFile))
                    {
                        try
                        {
                            File.Delete(tempFile);
                        }
                        catch
                        {
                        }
                    }

                }

            }


            if (noSymbols.Count == 0 && noSourceLink.Count == 0)
            {
                if(filesWithPdb.Count == 0)
                {
                    Result = SymbolValidationResult.NothingToValidate;
                    ErrorMessage = "No files found to validate";
                }
                else if(requireExternal)
                {
                    Result = SymbolValidationResult.ValidExternal;
                    ErrorMessage = null;
                }
                else
                {
                    Result = SymbolValidationResult.Valid;
                    ErrorMessage = null;
                }
            }
            else
            {
                var found = false;
                var sb = new StringBuilder();
                if (noSourceLink.Count > 0)
                {                    
                    Result = SymbolValidationResult.NoSourceLink;

                    sb.AppendLine($"Missing Source Link for:\n{string.Join("\n", noSourceLink.Select(p => p.Path)) }");
                    found = true;
                }

                if (noSymbols.Count > 0) // No symbols "wins" as it's more severe
                {
                    Result = SymbolValidationResult.NoSymbols;

                    if (found)
                        sb.AppendLine();

                    sb.AppendLine($"Missing Symbols for:\n{string.Join("\n", noSymbols.Select(p => p.Path)) }");
                }

                ErrorMessage = sb.ToString();
            }
            
        }

        static Stream MakeSeekable(Stream stream, bool disposeOriginal = false)
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


        public SymbolValidationResult Result
        {
            get; private set;
        }

        public string? ErrorMessage
        {
            get; private set;
        }

        private class FileWithPdb
        {
            public PackageFile Primary { get; set; }
            public PackageFile Pdb { get; set; }
        }
    }
}
