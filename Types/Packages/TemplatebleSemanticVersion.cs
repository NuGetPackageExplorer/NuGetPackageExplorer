using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using NuGet.Versioning;

namespace NuGetPe
{
    /// <summary>
    /// Version with optional templates values (tokens), like $version$
    /// </summary>
    public class TemplatebleSemanticVersion : IComparable<TemplatebleSemanticVersion>
    {
        /// <summary>
        /// Versionstring with isn't a valid <see cref="SemanticVersion"/> due to $token etc
        /// </summary>
        private readonly string _versionWithTokens;

        /// <summary>
        /// If filled, then valid version without tokens
        /// </summary>
        private SemanticVersion _version;

        /// <summary>Initializes a new instance of the <see cref="T:System.Object" /> class.</summary>
        public TemplatebleSemanticVersion(SemanticVersion version)
        {
            _version = version;
        }

        /// <summary>Initializes a new instance of the <see cref="T:System.Object" /> class.</summary>
        private TemplatebleSemanticVersion(string versionWithTokens)
        {
            _versionWithTokens = versionWithTokens;
            _version = new SemanticVersion(0, 0, 0, 0);
        }

        /// <summary>Initializes a new instance of the <see cref="T:System.Object" /> class.</summary>
        public TemplatebleSemanticVersion(int major, int minor, int build, int revision)
        {
            _version = new SemanticVersion(major, minor, build, revision);
        }

        public static bool TryParse(string version, out TemplatebleSemanticVersion templatebleSemanticVersion)
        {
            templatebleSemanticVersion = Parse(version);

            //never fails
            return true;
        }

        public static TemplatebleSemanticVersion Parse(string version)
        {
            SemanticVersion semanticVersion;
            if (SemanticVersion.TryParse(version, out semanticVersion))
            {
                return new TemplatebleSemanticVersion(semanticVersion);
            }
            return new TemplatebleSemanticVersion(version);
        }

        #region Relational members

        /// <summary>Compares the current instance with another object of the same type and returns an integer that indicates whether the current instance precedes, follows, or occurs in the same position in the sort order as the other object. </summary>
        /// <returns>A value that indicates the relative order of the objects being compared. The return value has these meanings: Value Meaning Less than zero This instance precedes <paramref name="other" /> in the sort order.  Zero This instance occurs in the same position in the sort order as <paramref name="other" />. Greater than zero This instance follows <paramref name="other" /> in the sort order. </returns>
        /// <param name="other">An object to compare with this instance. </param>
        public int CompareTo(TemplatebleSemanticVersion other)
        {
            if (ReferenceEquals(this, other)) return 0;
            if (ReferenceEquals(null, other)) return 1;
            var versionWithTokensComparison = string.Compare(_versionWithTokens, other._versionWithTokens, StringComparison.Ordinal);
            if (versionWithTokensComparison != 0) return versionWithTokensComparison;
            return Comparer<SemanticVersion>.Default.Compare(_version, other._version);
        }

        #endregion

        #region Equality members

        protected bool Equals(TemplatebleSemanticVersion other)
        {
            return string.Equals(_versionWithTokens, other._versionWithTokens) && Equals(_version, other._version);
        }

        /// <summary>Determines whether the specified object is equal to the current object.</summary>
        /// <returns>true if the specified object  is equal to the current object; otherwise, false.</returns>
        /// <param name="obj">The object to compare with the current object. </param>
        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj)) return false;
            if (ReferenceEquals(this, obj)) return true;
            if (obj.GetType() != this.GetType()) return false;
            return Equals((TemplatebleSemanticVersion) obj);
        }

        /// <summary>Serves as the default hash function. </summary>
        /// <returns>A hash code for the current object.</returns>
        public override int GetHashCode()
        {
            unchecked
            {
                return ((_versionWithTokens != null ? _versionWithTokens.GetHashCode() : 0) * 397) ^ (_version != null ? _version.GetHashCode() : 0);
            }
        }

        #endregion

        public SemanticVersion SemanticVersion
        {
            get { return _version; }
        }

        public string SpecialVersion
        {
            get
            {
                if (_versionWithTokens != null)
                {
                    return _versionWithTokens;
                }
                return _version?.SpecialVersion;
            }
        }

        #region Overrides of Object

        /// <summary>Returns a string that represents the current object.</summary>
        /// <returns>A string that represents the current object.</returns>
        public override string ToString()
        {
            if (_versionWithTokens != null)
            {
                return _versionWithTokens;
            }

            return _version.ToString();
        }

        #endregion
    }
}
