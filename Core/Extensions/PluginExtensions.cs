using System;
using System.IO;

namespace NuGetPe
{
    public static class PluginExtensions
    {
        public static int UnpackPackage(this IPackage package, string sourceDirectory, string targetRootDirectory)
        {
            if (package is null)
                throw new ArgumentNullException(nameof(package));
            if (sourceDirectory == null)
            {
                throw new ArgumentNullException(nameof(sourceDirectory));
            }

            if (targetRootDirectory == null)
            {
                throw new ArgumentNullException(nameof(targetRootDirectory));
            }

            if (!sourceDirectory.EndsWith("\\", StringComparison.OrdinalIgnoreCase))
            {
                sourceDirectory += "\\";
            }

            var numberOfFilesCopied = 0;
            foreach (var file in package.GetFiles())
            {
                if (file.Path.StartsWith(sourceDirectory, StringComparison.OrdinalIgnoreCase))
                {
                    var suffixPath = file.Path.Substring(sourceDirectory.Length);
                    var targetPath = Path.Combine(targetRootDirectory, suffixPath);

                    using (var stream = File.Open(targetPath, FileMode.Create, FileAccess.Write, FileShare.Read))
                    {
                        using var packageStream = file.GetStream();
                        packageStream.CopyTo(stream);
                    }
                    File.SetLastWriteTime(targetPath, file.LastWriteTime.DateTime);

                    numberOfFilesCopied++;
                }
            }

            return numberOfFilesCopied;
        }
    }
}
