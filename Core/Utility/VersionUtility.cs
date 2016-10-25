using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Runtime.Versioning;
using System.Text.RegularExpressions;
using NuGet.Resources;

namespace NuGet
{
    public static class VersionUtility
    {
        private const string NetFrameworkIdentifier = ".NETFramework";
        private const string NetCoreFrameworkIdentifier = ".NETCore";
        private const string PortableFrameworkIdentifier = ".NETPortable";

        [SuppressMessage("Microsoft.Security", "CA2104:DoNotDeclareReadOnlyMutableReferenceTypes")]
        public static readonly FrameworkName UnsupportedFrameworkName = new FrameworkName("Unsupported", new Version());
        private static readonly Version _emptyVersion = new Version();

        private static readonly Dictionary<string, string> _knownIdentifiers = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
            {
                { "DNX", "DNX" },
                { "dotnet", "dotnet" },
                { "NET", NetFrameworkIdentifier },
                { ".NET", NetFrameworkIdentifier },
                { "NETFramework", NetFrameworkIdentifier },
                { ".NETFramework", NetFrameworkIdentifier },
                { "NETCore", NetCoreFrameworkIdentifier},
                { ".NETCore", NetCoreFrameworkIdentifier},
                { "NETPlatform", "NETPlatform"},
                { ".NETPlatform", "NETPlatform"},
                { ".NETStandard", ".NETStandard" },
                { "NETStandard", ".NETStandard" },
                { "NETStandardApp", ".NETStandard App" },
                { ".NETStandardApp", ".NETStandard App" },
                { "WinRT", NetCoreFrameworkIdentifier},     // 'WinRT' is now deprecated. Use 'Windows' or 'win' instead.
                { ".NETMicroFramework", ".NETMicroFramework" },
                { "netmf", ".NETMicroFramework" },
                { "SL", "Silverlight" },
                { "Silverlight", "Silverlight" },
                { ".NETPortable", PortableFrameworkIdentifier },
                { "NETPortable", PortableFrameworkIdentifier },
                { "portable", PortableFrameworkIdentifier },
                { "wp", "WindowsPhone" },
                { "WindowsPhone", "WindowsPhone" },
                { "WindowsPhoneApp", "WindowsPhoneApp"},
                { "WindowsPhoneAppx", "WindowsPhoneAppx"},
                { "wpa", "WindowsPhoneAppx"},
                { "Windows", "Windows" },
                { "win", "Windows" },
                { "MonoAndroid", "MonoAndroid" },
                { "MonoTouch", "MonoTouch" },
                { "MonoMac", "MonoMac" },
                { "Mono", "Mono" },
                { "native", "native" },
                { "dnxcore", "DNXCore" },
                { "Xamarin.ios", "Xamarin iOS" },
                { "Xamarin.mac", "Xamarin Mac" },
                { "xamarinmac", "Xamarin Mac"},
                { "xamarinios", "Xamarin iOs"},
                { "xamarinpsthree", "Xamarin on Playstation 3"},
                { "xamarinpsfour", "Xamarin on Playstation 4"},
                { "xamarinpsvita", "Xamarin on PS Vita"},
                { "xamarinwatchos", "Xamarin for Watch OS"},
                { "xamarintvos", "Xamarin for TV OS"},
                { "xamarinxboxthreesixty", "Xamarin for XBox 360"},
                { "xamarinxboxone", "Xamarin for XBox One"},
                { "uap", "Windows 10 Universal App Platform" },
            };

        private static readonly Dictionary<string, string> _knownProfiles = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
            {
                {"Client", "Client"},
                {"WP", "WindowsPhone"},
                {"WP71", "WindowsPhone71"},
                {"CF", "CompactFramework"},
                {"Full", String.Empty}
            };

        private static readonly Dictionary<string, string> _identifierToFrameworkFolder = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase) {
            { NetFrameworkIdentifier, "net" },
            { ".NETMicroFramework", "netmf" },
            { "Silverlight", "sl" },
            { ".NETCore", "win"},
            { "Windows", "win"},
            { ".NETPortable", "portable" },
            { "WindowsPhone", "wp"},
            { "WindowsPhoneAppx", "wpa" }
        };

        private static readonly Dictionary<string, string> _identifierToProfileFolder = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase) {
            { "WindowsPhone", "wp" },
            { "WindowsPhone71", "wp71" },
            { "CompactFramework", "cf" }
        };

        private static Version DefaultTargetFrameworkVersion
        {
            get
            {
                // We need to parse the version name out from the mscorlib's assembly name since
                // we can't call GetName() in medium trust
                return typeof(string).Assembly.GetNameSafe().Version;
            }
        }

        public static FrameworkName DefaultTargetFramework
        {
            get { return new FrameworkName(NetFrameworkIdentifier, DefaultTargetFrameworkVersion); }
        }

        public static Version ParseOptionalVersion(string version)
        {
            Version versionValue;
            if (!String.IsNullOrEmpty(version) && Version.TryParse(version, out versionValue))
            {
                return versionValue;
            }
            return null;
        }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Design", "CA1021:AvoidOutParameters", MessageId = "1#")]
        public static FrameworkName ParseFrameworkNameFromFilePath(string filePath, out string effectivePath)
        {
            var knownFolders = new string[]
            {
                Constants.ContentDirectory,
                Constants.LibDirectory,
                Constants.ToolsDirectory
            };

            for (int i = 0; i < knownFolders.Length; i++)
            {
                string folderPrefix = knownFolders[i] + System.IO.Path.DirectorySeparatorChar;
                if (filePath.Length > folderPrefix.Length &&
                    filePath.StartsWith(folderPrefix, StringComparison.OrdinalIgnoreCase))
                {
                    string frameworkPart = filePath.Substring(folderPrefix.Length);

                    try
                    {
                        return VersionUtility.ParseFrameworkFolderName(
                            frameworkPart,
                            strictParsing: knownFolders[i] != Constants.ContentDirectory,
                            effectivePath: out effectivePath);
                    }
                    catch (ArgumentException)
                    {
                        // if the parsing fails, we treat it as if this file
                        // doesn't have target framework.
                        effectivePath = frameworkPart;
                        return null;
                    }
                }
            }

            effectivePath = filePath;
            return null;
        }

        /// <summary>
        /// This function tries to normalize a string that represents framework version names into
        /// something a framework name that the package manager understands.
        /// </summary>
        public static FrameworkName ParseFrameworkName(string frameworkName)
        {
            if (frameworkName == null)
            {
                throw new ArgumentNullException("frameworkName");
            }

            // {Identifier}{Version}-{Profile}

            // Split the framework name into 3 parts, identifier, version and profile.
            string identifierPart;
            string versionPart = null;

            string[] parts = frameworkName.Split('-');

            if (parts.Length > 2)
            {
                throw new ArgumentException(NuGetResources.InvalidFrameworkNameFormat, "frameworkName");
            }

            string frameworkNameAndVersion = parts.Length > 0 ? parts[0].Trim() : null;
            string profilePart = parts.Length > 1 ? parts[1].Trim() : null;

            if (String.IsNullOrEmpty(frameworkNameAndVersion))
            {
                throw new ArgumentException(NuGetResources.MissingFrameworkName, "frameworkName");
            }

            // If we find a version then we try to split the framework name into 2 parts
            Match versionMatch = Regex.Match(frameworkNameAndVersion, @"\d+");

            if (versionMatch.Success)
            {
                identifierPart = frameworkNameAndVersion.Substring(0, versionMatch.Index).Trim();
                versionPart = frameworkNameAndVersion.Substring(versionMatch.Index).Trim();
            }
            else
            {
                // Otherwise we take the whole name as an identifier
                identifierPart = frameworkNameAndVersion.Trim();
            }

            if (!String.IsNullOrEmpty(identifierPart))
            {
                string parsedIdentifierPart;
                // Try to nomalize the identifier to a known identifier
                if (_knownIdentifiers.TryGetValue(identifierPart, out parsedIdentifierPart))
                {
                    identifierPart = parsedIdentifierPart;
                }
                //if not supported, just continue. Otherwise, 2 different platforms which lead to unsupported could lead to a crash (reference already exists). 
                //Also letting NPE crashing because of missing platforms isn't needed.
            }

            if (!String.IsNullOrEmpty(profilePart))
            {
                string knownProfile;
                if (_knownProfiles.TryGetValue(profilePart, out knownProfile))
                {
                    profilePart = knownProfile;
                }
            }

            Version version;
            // We support version formats that are integers (40 becomes 4.0)
            int versionNumber;
            if (Int32.TryParse(versionPart, out versionNumber))
            {
                // Remove the extra numbers
                if (versionPart.Length > 4)
                {
                    versionPart = versionPart.Substring(0, 4);
                }

                // Make sure it has at least 2 digits so it parses as a valid version
                versionPart = versionPart.PadRight(2, '0');
                versionPart = String.Join(".", versionPart.Select(ch => ch.ToString(CultureInfo.InvariantCulture)));
            }

            // If we can't parse the version then use the default
            if (!Version.TryParse(versionPart, out version))
            {
                // We failed to parse the version string once more. So we need to decide if this is unsupported or if we use the default version.
                // This framework is unsupported if:
                // 1. The identifier part of the framework name is null.
                // 2. The version part is not null.
                if (String.IsNullOrEmpty(identifierPart) || !String.IsNullOrEmpty(versionPart))
                {
                    return UnsupportedFrameworkName;
                }

                version = _emptyVersion;
            }

            if (String.IsNullOrEmpty(identifierPart))
            {
                identifierPart = NetFrameworkIdentifier;
            }

            // if this is a .NET Portable framework name, validate the profile part to ensure it is valid
            if (identifierPart.Equals(PortableFrameworkIdentifier, StringComparison.OrdinalIgnoreCase))
            {
                bool isValid = ValidatePortableFrameworkProfilePart(profilePart);
                if (!isValid)
                {
                    return UnsupportedFrameworkName;
                }
            }

            return new FrameworkName(identifierPart, version, profilePart);
        }

        internal static bool ValidatePortableFrameworkProfilePart(string profilePart)
        {
            if (String.IsNullOrEmpty(profilePart))
            {
                return false;
            }

            if (profilePart.Contains('-'))
            {
                return false;
            }

            if (profilePart.Contains(' '))
            {
                return false;
            }

            string[] parts = profilePart.Split('+');
            if (parts.Any(p => String.IsNullOrEmpty(p)))
            {
                return false;
            }

            // Prevent portable framework inside a portable framework - Inception
            if (parts.Any(p => p.StartsWith("portable", StringComparison.OrdinalIgnoreCase)) ||
                parts.Any(p => p.StartsWith("NETPortable", StringComparison.OrdinalIgnoreCase)) ||
                parts.Any(p => p.StartsWith(".NETPortable", StringComparison.OrdinalIgnoreCase)))
            {
                return false;
            }

            return true;
        }

        /// <summary>
        /// The version string is either a simple version or an arithmetic range
        /// e.g.
        ///      1.0         --> 1.0 ≤ x
        ///      (,1.0]      --> x ≤ 1.0
        ///      (,1.0)      --> x &lt; 1.0
        ///      [1.0]       --> x == 1.0
        ///      (1.0,)      --> 1.0 &lt; x
        ///      (1.0, 2.0)   --> 1.0 &lt; x &lt; 2.0
        ///      [1.0, 2.0]   --> 1.0 ≤ x ≤ 2.0
        /// </summary>
        public static IVersionSpec ParseVersionSpec(string value)
        {
            IVersionSpec versionInfo;
            if (!TryParseVersionSpec(value, out versionInfo))
            {
                throw new ArgumentException(
                    String.Format(CultureInfo.CurrentCulture,
                                  NuGetResources.InvalidVersionString, value));
            }

            return versionInfo;
        }

        public static bool TryParseVersionSpec(string value, out IVersionSpec result)
        {
            if (value == null)
            {
                throw new ArgumentNullException("value");
            }

            var versionSpec = new VersionSpec();
            value = value.Trim();

            // First, try to parse it as a plain version string
            SemanticVersion version;
            if (SemanticVersion.TryParse(value, out version))
            {
                // A plain version is treated as an inclusive minimum range
                result = new VersionSpec
                {
                    MinVersion = version,
                    IsMinInclusive = true
                };

                return true;
            }

            // It's not a plain version, so it must be using the bracket arithmetic range syntax

            result = null;

            // Fail early if the string is too short to be valid
            if (value.Length < 3)
            {
                return false;
            }

            // The first character must be [ ot (
            switch (value.First())
            {
                case '[':
                    versionSpec.IsMinInclusive = true;
                    break;
                case '(':
                    versionSpec.IsMinInclusive = false;
                    break;
                default:
                    return false;
            }

            // The last character must be ] ot )
            switch (value.Last())
            {
                case ']':
                    versionSpec.IsMaxInclusive = true;
                    break;
                case ')':
                    versionSpec.IsMaxInclusive = false;
                    break;
                default:
                    return false;
            }

            // Get rid of the two brackets
            value = value.Substring(1, value.Length - 2);

            // Split by comma, and make sure we don't get more than two pieces
            string[] parts = value.Split(',');
            if (parts.Length > 2)
            {
                return false;
            }
            else if (parts.All(String.IsNullOrEmpty))
            {
                // If all parts are empty, then neither of upper or lower bounds were specified. Version spec is of the format (,]
                return false;
            }

            // If there is only one piece, we use it for both min and max
            string minVersionString = parts[0];
            string maxVersionString = (parts.Length == 2) ? parts[1] : parts[0];

            // Only parse the min version if it's non-empty
            if (!String.IsNullOrWhiteSpace(minVersionString))
            {
                if (!TryParseVersion(minVersionString, out version))
                {
                    return false;
                }
                versionSpec.MinVersion = version;
            }

            // Same deal for max
            if (!String.IsNullOrWhiteSpace(maxVersionString))
            {
                if (!TryParseVersion(maxVersionString, out version))
                {
                    return false;
                }
                versionSpec.MaxVersion = version;
            }

            // Successful parse!
            result = versionSpec;
            return true;
        }

        public static string GetFrameworkString(FrameworkName frameworkName)
        {
            string name = frameworkName.Identifier + frameworkName.Version;
            if (String.IsNullOrEmpty(frameworkName.Profile))
            {
                return name;
            }
            return name + "-" + frameworkName.Profile;
        }

        public static FrameworkName ParseFrameworkFolderName(string path)
        {
            string effectivePath;
            return ParseFrameworkFolderName(path, strictParsing: true, effectivePath: out effectivePath);
        }

        /// <summary>
        /// Parses the specified string into FrameworkName object.
        /// </summary>
        /// <param name="path">The string to be parse.</param>
        /// <param name="strictParsing">if set to <c>true</c>, parse the first folder of path even if it is unrecognized framework.</param>
        /// <param name="effectivePath">returns the path after the parsed target framework</param>
        /// <returns></returns>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Design", "CA1021:AvoidOutParameters", MessageId = "2#")]
        public static FrameworkName ParseFrameworkFolderName(string path, bool strictParsing, out string effectivePath)
        {
            // The path for a reference might look like this for assembly foo.dll:            
            // foo.dll
            // sub\foo.dll
            // {FrameworkName}{Version}\foo.dll
            // {FrameworkName}{Version}\sub1\foo.dll
            // {FrameworkName}{Version}\sub1\sub2\foo.dll

            // Get the target framework string if specified
            string targetFrameworkString = Path.GetDirectoryName(path).Split(Path.DirectorySeparatorChar).First();

            effectivePath = path;

            if (String.IsNullOrEmpty(targetFrameworkString))
            {
                return null;
            }

            var targetFramework = ParseFrameworkName(targetFrameworkString);
            if (strictParsing || targetFramework != UnsupportedFrameworkName)
            {
                // skip past the framework folder and the character \
                effectivePath = path.Substring(targetFrameworkString.Length + 1);
                return targetFramework;
            }

            return null;
        }

        public static bool TryParseVersion(string versionValue, out SemanticVersion version)
        {
            if (!SemanticVersion.TryParse(versionValue, out version))
            {
                // Support integer version numbers (i.e 1 -> 1.0)
                int versionNumber;
                if (Int32.TryParse(versionValue, out versionNumber) && versionNumber > 0)
                {
                    version = new SemanticVersion(versionNumber, 0, 0, 0);
                }
            }
            return version != null;
        }

        public static string GetShortFrameworkName(FrameworkName frameworkName)
        {
            string name;
            if (!_identifierToFrameworkFolder.TryGetValue(frameworkName.Identifier, out name))
            {
                name = frameworkName.Identifier;
            }

            // only show version part if it's > 0.0.0.0
            if (frameworkName.Version > new Version())
            {
                // Remove the . from versions
                name += frameworkName.Version.ToString().Replace(".", String.Empty);
            }

            if (String.IsNullOrEmpty(frameworkName.Profile))
            {
                return name;
            }

            string profile;
            if (!_identifierToProfileFolder.TryGetValue(frameworkName.Profile, out profile))
            {
                profile = frameworkName.Profile;
            }

            return name + "-" + profile;
        }
    }
}