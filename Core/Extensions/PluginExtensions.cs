using System;
using System.IO;

namespace NuGetPe
{
    public static class PluginExtensions
    {
        public static int UnpackPackage(this IPackage package, string sourceDirectory, string targetRootDirectory)
        {
            if (sourceDirectory == null)
            {
                throw new ArgumentNullException("sourceDirectory");
            }

            if (targetRootDirectory == null)
            {
                throw new ArgumentNullException("targetRootDirectory");
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

                    numberOfFilesCopied++;
                }
            }

            return numberOfFilesCopied;
        }
    }
}
