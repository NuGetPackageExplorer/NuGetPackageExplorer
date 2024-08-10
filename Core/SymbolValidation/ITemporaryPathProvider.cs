namespace NuGetPe
{
    /// <summary>
    /// Generates a path for a temporary file. Can be used with <see cref="TemporaryFile"/> to allow more control for
    /// the naming of temporary files.
    /// </summary>
    public interface ITemporaryPathProvider
    {
        /// <summary>
        /// Provides a path, given optional context values. A fully qualified path must be returned. The caller of this
        /// method should be able to write to the returned path. The directory must exist.
        /// </summary>
        /// <param name="package">The package related to the temp file that will be created.</param>
        /// <param name="fileName">The desired file name for the temporary file.</param>
        /// <returns>A full path to where the temporary file will be stored.</returns>
        public string GetPath(IPackage? package, string? fileName);
    }
}
