using System;
using System.Globalization;
using System.Text;

namespace NuGet {
    public class VersionSpec : IVersionSpec {
        public Version MinVersion { get; set; }
        public bool IsMinInclusive { get; set; }
        public Version MaxVersion { get; set; }
        public bool IsMaxInclusive { get; set; }

        public override string ToString() {
            if (MinVersion != null && IsMinInclusive && MaxVersion == null && !IsMaxInclusive) {
                return MinVersion.ToString();
            }

            if (MinVersion != null && MaxVersion != null && MinVersion == MaxVersion && IsMinInclusive && IsMaxInclusive) {
                return "[" + MinVersion + "]";
            }

            var versionBuilder = new StringBuilder();
            if (IsMinInclusive) {
                versionBuilder.Append("[");
            }
            else {
                versionBuilder.Append("(");
            }

            versionBuilder.AppendFormat(CultureInfo.InvariantCulture, "{0}, {1}", MinVersion, MaxVersion);

            if (IsMaxInclusive) {
                versionBuilder.Append("]");
            }
            else {
                versionBuilder.Append(")");
            }

            return versionBuilder.ToString(); 
        }
    }
}