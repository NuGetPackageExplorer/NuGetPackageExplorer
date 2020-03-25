using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using NuGet.Packaging;

using NuGetPe;
using NuGetPe.AssemblyMetadata;

namespace PackageExplorerViewModel
{
    public class SymbolValidator : INotifyPropertyChanged
    {
        private readonly PackageViewModel _packageViewModel;

        public SymbolValidator(PackageViewModel packageViewModel)
        {
            _packageViewModel = packageViewModel ?? throw new ArgumentNullException(nameof(packageViewModel));
            _packageViewModel.PropertyChanged += _packageViewModel_PropertyChanged;
        }

        private void _packageViewModel_PropertyChanged(object? sender, PropertyChangedEventArgs e)
        {
            if(e.PropertyName == null)
            {
                HasSourceLink = null;
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

                await Task.Run(() => CalculateValidity(files));
            }
            catch(Exception e)
            {
                DiagnosticsClient.TrackException(e);
            }
            finally
            {
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(HasSourceLink)));
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(ErrorMessage)));
            }
        }

        private void CalculateValidity(IReadOnlyList<PackageFile> files)
        {
            var filesWithPdb = (from pf in files
                                let ext = Path.GetExtension(pf.Path)
                                where ".dll".Equals(ext, StringComparison.OrdinalIgnoreCase) ||
                                      ".exe".Equals(ext, StringComparison.OrdinalIgnoreCase) ||
                                      ".winmd".Equals(ext, StringComparison.OrdinalIgnoreCase)
                                select new
                                {
                                    primary = pf,
                                    pdb = pf.GetAssociatedFiles().FirstOrDefault(af => ".pdb".Equals(Path.GetExtension(af.Path), StringComparison.OrdinalIgnoreCase))
                                })
                                .ToList();


            var missing = new List<PackageFile>();

            foreach(var file in filesWithPdb)
            {
                // If we have a PDB, try loading that first. If not, may be embedded. Ottherwise, missing for now
                // TODO: Check for symbol server and SNUPKG later

                if(file.pdb != null)
                {
                    var peStream = MakeSeekable(file.primary.GetStream(), true);
                    try
                    {

                        // This might throw an exception because we don't know if it's a full PDB or portable
                        // Try anyway in case it succeeds as a ppdb
                        try
                        {
                            using (var stream = MakeSeekable(file.pdb.GetStream(), true))
                            {
                                var data = AssemblyMetadataReader.ReadDebugData(peStream, stream);

                                if(!data.HasSourceLink)
                                {
                                    missing.Add(file.primary);
                                }
                            }

                        }
                        catch (ArgumentNullException)
                        {
                            missing.Add(file.primary);
                        }
                    }
                    finally
                    {
                        peStream?.Dispose();
                    }
                }
                else
                {
                    var tempFile = Path.GetTempFileName();
                    try
                    {                        
                        using (var str = file.primary.GetStream())
                        using (var fileStream = File.OpenWrite(tempFile))
                        {
                            str.CopyTo(fileStream);
                        }

                        var assemblyMetadata = AssemblyMetadataReader.ReadMetaData(tempFile);

                        if (assemblyMetadata?.DebugData?.HasSourceLink != true)
                        {
                            missing.Add(file.primary);
                        }
                    }
                    catch
                    {
                        missing.Add(file.primary);
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

            if(missing.Count == 0)
            {
                HasSourceLink = true;
                ErrorMessage = null;
            }
            else
            {
                HasSourceLink = false;
                ErrorMessage = $"Missing Source Link for {string.Join(", ", missing.Select(p => p.Path)) }";
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


        public bool? HasSourceLink
        {
            get; private set;
        }

        public string? ErrorMessage
        {
            get; private set;
        }
    }
}
