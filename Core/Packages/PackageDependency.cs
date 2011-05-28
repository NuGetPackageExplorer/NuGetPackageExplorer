using System;
using System.Globalization;
using System.Text;

namespace NuGet {
    public class PackageDependency {
        private const string LessThanOrEqualTo = "\u2264";
        private const string GreaterThanOrEqualTo = "\u2265";

        public PackageDependency(string id)
            : this(id, null) {
        }

        public PackageDependency(string id, IVersionSpec versionSpec) {
            if (String.IsNullOrEmpty(id)) {
                throw new ArgumentNullException("id");
            }
            Id = id;
            VersionSpec = versionSpec;
        }

        public string Id {
            get;
            private set;
        }

        public IVersionSpec VersionSpec {
            get;
            private set;
        }

        public override string ToString() {
            if (VersionSpec == null) {
                return Id;
            }

            if (VersionSpec.MinVersion != null && VersionSpec.IsMinInclusive && VersionSpec.MaxVersion == null && !VersionSpec.IsMaxInclusive) {
                return String.Format(CultureInfo.InvariantCulture, "{0} ({1} {2})", Id, GreaterThanOrEqualTo, VersionSpec.MinVersion);
            }

            if (VersionSpec.MinVersion != null && VersionSpec.MaxVersion != null && VersionSpec.MinVersion == VersionSpec.MaxVersion && VersionSpec.IsMinInclusive && VersionSpec.IsMaxInclusive) {
                return String.Format(CultureInfo.InvariantCulture, "{0} (= {1})", Id, VersionSpec.MinVersion);
            }

            var versionBuilder = new StringBuilder();
            if (VersionSpec.MinVersion != null) {
                if (VersionSpec.IsMinInclusive) {
                    versionBuilder.AppendFormat(CultureInfo.InvariantCulture, "({0} ", GreaterThanOrEqualTo);
                }
                else {
                    versionBuilder.Append("(> ");
                }
                versionBuilder.Append(VersionSpec.MinVersion);
            }

            if (VersionSpec.MaxVersion != null) {
                if (versionBuilder.Length == 0) {
                    versionBuilder.Append("(");
                }
                else {
                    versionBuilder.Append(" && ");
                }

                if (VersionSpec.IsMaxInclusive) {
                    versionBuilder.AppendFormat(CultureInfo.InvariantCulture, "{0} ", LessThanOrEqualTo);
                }
                else {
                    versionBuilder.Append("< ");
                }
                versionBuilder.Append(VersionSpec.MaxVersion);
            }

            if (versionBuilder.Length > 0) {
                versionBuilder.Append(")");
            }

            return Id + " " + versionBuilder;
        }
    }
}
