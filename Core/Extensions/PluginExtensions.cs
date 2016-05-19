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

            int numberOfFilesCopied = 0;
            foreach (IPackageFile file in package.GetFiles())
            {
                if (file.Path.StartsWith(sourceDirectory, StringComparison.OrdinalIgnoreCase))
                {
                    string suffixPath = file.Path.Substring(sourceDirectory.Length);
                    string targetPath = Path.Combine(targetRootDirectory, suffixPath);

                    using (FileStream stream = File.OpenWrite(targetPath))
                    {
                        using (Stream packageStream = file.GetStream())
                        {
                            packageStream.CopyTo(stream);
                        }
                    }

                    numberOfFilesCopied++;
                }
            }

            return numberOfFilesCopied;
        }
    }
}