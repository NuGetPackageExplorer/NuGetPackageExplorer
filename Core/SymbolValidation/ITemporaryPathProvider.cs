using System.IO;

namespace NuGetPe
{
    /// <summary>
    /// Generates temporary files.
    /// </summary>
    public interface ITemporaryFileProvider
    {
        /// <summary>
        /// Provides a <see cref="TemporaryFile"/>, given a readable stream and optional context values.
        /// </summary>
        /// <param name="stream">The stream that will be written to a temporary file on disk.</param>
        /// <param name="package">The package related to the temporary file that will be created.</param>
        /// <param name="fileName">The desired file name for the temporary file.</param>
        /// <param name="part">The part of the package that will be written to the temporary file, for context purposes.</param>
        /// <returns>The temporary file, written to disk.</returns>
        public TemporaryFile GetTemporaryFile(Stream stream, IPackage? package, string? fileName, IPart? part);
    }
}
