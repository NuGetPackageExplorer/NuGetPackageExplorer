using System;
using System.Globalization;
using System.Text.RegularExpressions;

namespace NuGet {
    /// <summary>
    /// A hybrid implementation of SemVer that supports semantic versioning as described at http://semver.org while not strictly enforcing it to 
    /// allow older 4-digit versioning schemes to continue working.
    /// </summary>
    [Serializable]
    public sealed class SemanticVersion : IComparable, IComparable<SemanticVersion>, IEquatable<SemanticVersion> {
        private const string SemanticVersionRegex = @"^(?<Version>\d+(\s*\.\s*\d+){0,3})(?<Release>[a-z][0-9a-z-]{0,19})?$";
        private const string StrictSemanticVersionRegex = @"^(?<Version>\d+(\.\d+){2})(?<Release>[a-z][0-9a-z-]{0,19})?$";
        private readonly string _originalString; 

        public SemanticVersion(string version)
            : this(Parse(version)) {
            // The constructor normalizes the version string so that it we do not need to normalize it every time we need to operate on it. 
            // The original string represents the original form in which the version is represented to be used when printing.
            _originalString = version;
        }

        public SemanticVersion(int major, int minor, int build, int revision)
            : this(new Version(major, minor, build, revision)) {
        }

        public SemanticVersion(int major, int minor, int build, string specialVersion)
            : this(new Version(major, minor, build), specialVersion) {
        }

        public SemanticVersion(Version version)
            : this(version, String.Empty) {
        }

        public SemanticVersion(Version version, string specialVersion)
            : this(version, specialVersion, null) {
        }

        private SemanticVersion(Version version, string specialVersion, string originalString) {
            if (version == null) {
                throw new ArgumentNullException("version");
            }
            if (specialVersion != null && specialVersion.Length > 20)
            {
                throw new ArgumentException("Special version part cannot exceed 20 characters.", "specialVersion");
            }

            Version = NormalizeVersionValue(version);
            SpecialVersion = specialVersion ?? String.Empty;
            _originalString = String.IsNullOrEmpty(originalString) ? version.ToString() + specialVersion : originalString;
        }

        internal SemanticVersion(SemanticVersion semVer) {
            _originalString = semVer.ToString();
            Version = semVer.Version;
            SpecialVersion = semVer.SpecialVersion;
        }

        /// <summary>
        /// Gets the normalized version portion.
        /// </summary>
        public Version Version {
            get;
            private set;
        }

        /// <summary>
        /// Gets the optional special version.
        /// </summary>
        public string SpecialVersion {
            get;
            private set;
        }

        /// <summary>
        /// Parses a version string using loose semantic versioning rules that allows 2-4 version components followed by an optional special version.
        /// </summary>
        public static SemanticVersion Parse(string version) {
            if (String.IsNullOrEmpty(version)) {
                throw new ArgumentException("Cannot be null or empty.", "version");
            }

            SemanticVersion semVer;
            if (!TryParse(version, out semVer)) {
                throw new ArgumentException(String.Format(CultureInfo.CurrentCulture, "'{0}' is not a valid version string.", version), "version");
            }
            return semVer;
        }

        /// <summary>
        /// Parses a version string using loose semantic versioning rules that allows 2-4 version components followed by an optional special version.
        /// </summary>
        public static bool TryParse(string version, out SemanticVersion value) {
            return TryParseInternal(version, SemanticVersionRegex, out value);
        }

        /// <summary>
        /// Parses a version string using strict semantic versioning rules that allows exactly 3 components and an optional special version.
        /// </summary>
        public static bool TryParseStrict(string version, out SemanticVersion value) {
            return TryParseInternal(version, StrictSemanticVersionRegex, out value);
        }

        private static bool TryParseInternal(string version, string regex, out SemanticVersion semVer) {
            semVer = null;
            if (String.IsNullOrEmpty(version)) {
                return false;
            }
            var regexFlags = RegexOptions.Compiled | RegexOptions.IgnoreCase | RegexOptions.ExplicitCapture;
            var semVerRegex = new Regex(regex, regexFlags);

            var match = semVerRegex.Match(version.Trim());
            Version versionValue;
            if (!match.Success || !Version.TryParse(match.Groups["Version"].Value, out versionValue)) {
                return false;
            }

            semVer = new SemanticVersion(NormalizeVersionValue(versionValue), match.Groups["Release"].Value, version.Replace(" ", ""));
            return true;
        }

        /// <summary>
        /// Attempts to parse the version token as a SemanticVersion.
        /// </summary>
        /// <returns>An instance of SemanticVersion if it parses correctly, null otherwise.</returns>
        public static SemanticVersion ParseOptionalVersion(string version) {
            SemanticVersion semVer;
            TryParse(version, out semVer);
            return semVer;
        }

        private static Version NormalizeVersionValue(Version version) {
            return new Version(version.Major,
                               version.Minor,
                               Math.Max(version.Build, 0),
                               Math.Max(version.Revision, 0));
        }

        public int CompareTo(object obj) {
            if (Object.ReferenceEquals(obj, null)) {
                return 1;
            }
            SemanticVersion other = obj as SemanticVersion;
            if (other == null) {
                throw new ArgumentException("Type to compare must be an instance of SemanticVersion.", "obj");
            }
            return CompareTo(other);
        }

        public int CompareTo(SemanticVersion other) {
            if (Object.ReferenceEquals(other, null)) {
                return 1;
            }

            int result = Version.CompareTo(other.Version);

            if (result != 0) {
                return result;
            }

            bool empty = String.IsNullOrEmpty(SpecialVersion);
            bool otherEmpty = String.IsNullOrEmpty(other.SpecialVersion);
            if (empty && otherEmpty) {
                return 0;
            }
            else if (empty) {
                return 1;
            }
            else if (otherEmpty) {
                return -1;
            }
            return StringComparer.OrdinalIgnoreCase.Compare(SpecialVersion, other.SpecialVersion);
        }

        public static bool operator ==(SemanticVersion version1, SemanticVersion version2) {
            if (Object.ReferenceEquals(version1, null)) {
                return Object.ReferenceEquals(version2, null);
            }
            return version1.Equals(version2);
        }

        public static bool operator !=(SemanticVersion version1, SemanticVersion version2) {
            return !(version1 == version2);
        }

        public static bool operator <(SemanticVersion version1, SemanticVersion version2) {
            if (version1 == null) {
                throw new ArgumentNullException("version1");
            }
            return version1.CompareTo(version2) < 0;
        }

        public static bool operator <=(SemanticVersion version1, SemanticVersion version2) {
            return (version1 == version2) || (version1 < version2);
        }

        public static bool operator >(SemanticVersion version1, SemanticVersion version2) {
            if (version1 == null) {
                throw new ArgumentNullException("version1");
            }
            return version2 < version1;
        }

        public static bool operator >=(SemanticVersion version1, SemanticVersion version2) {
            return (version1 == version2) || (version1 > version2);
        }

        public override string ToString() {
            return _originalString;
        }

        public bool Equals(SemanticVersion other) {
            return other != null && Version.Equals(other.Version) 
                                 && SpecialVersion.Equals(other.SpecialVersion, StringComparison.OrdinalIgnoreCase);
        }

        public override bool Equals(object obj) {
            SemanticVersion semVer = obj as SemanticVersion;
            return semVer != null && Equals(semVer);
        }

        public override int GetHashCode() {
            return Version.GetHashCode() * 3137 + 
                (SpecialVersion == null ? 0 : SpecialVersion.GetHashCode());
        }
    }
}
