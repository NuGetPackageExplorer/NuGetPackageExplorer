using System;
using NuGet.Packaging.Core;
using NuGet.Versioning;

namespace NuGetPe
{
    /// <summary>
    /// Encapsulates relevant NuGet package information.
    /// </summary>
    public class PackageInfo
    {
        /// <summary>
        /// The <see cref="PackageInfo"/> constructor.
        /// </summary>
        /// <param name="identity">
        /// The <see cref="PackageIdentity"/> that this PackageInfo will represent.
        /// </param>
        public PackageInfo(PackageIdentity identity)
        {
            Identity = identity ?? throw new ArgumentNullException(nameof(identity));
        }

        /// <summary>
        /// Gets the core package identity.
        /// </summary>
        public PackageIdentity Identity { get; }

        /// <summary>
        /// Gets the name of the package.
        /// </summary>
        public string Id => Identity.Id;

        /// <summary>
        /// Gets the <see cref="NuGet.Versioning.SemanticVersion"/> of the package.
        /// </summary>
        public NuGetVersion SemanticVersion => Identity.Version;

        /// <summary>
        /// Gets the full string representation of the version.
        /// </summary>
        public string Version => SemanticVersion.ToFullString();

        /// <summary>
        /// Gets or sets the full package description.
        /// </summary>
        public string? Description { get; set; }

        /// <summary>
        /// Gets or sets the short description of the package.
        /// </summary>
        public string? Summary { get; set; }

        /// <summary>
        /// Gets or sets the comma-separated list of packages authors.
        /// </summary>
        public string? Authors { get; set; }

        /// <summary>
        /// Gets or sets the number of package downloads.
        /// </summary>
        public long? DownloadCount { get; set; }

        /// <summary>
        /// Gets or sets the time of the packages publishing.
        /// </summary>
        public DateTimeOffset? Published { get; set; }

        /// <summary>
        /// Gets or sets the url to the package icon.
        /// </summary>
        public string? IconUrl { get; set; }

        /// <summary>
        /// Gets or sets the url to the package readme.
        /// </summary>
        public string? ReadmeUrl { get; set; }

        /// <summary>
        /// Gets or sets the url to the package license information.
        /// </summary>
        public string? LicenseUrl { get; set; }

        /// <summary>
        /// Gets or sets the url to the package website.
        /// </summary>
        public string? ProjectUrl { get; set; }

        /// <summary>
        /// Gets or sets the space-delimited list of tags and keywords that describe the package.
        /// </summary>
        public string? Tags { get; set; }

        /// <summary>
        /// Gets or sets the url to report abuse of the package.
        /// </summary>
        public string? ReportAbuseUrl { get; set; }

        /// <summary>
        /// Gets or sets whether or not the package ID has a reserved prefix.
        /// </summary>
        public bool IsPrefixReserved { get; set; }

        /// <summary>
        /// Gets or sets whether or not the package is hosted on a remote feed.
        /// </summary>
        public bool IsRemotePackage { get; set; }

        /// <summary>
        /// Gets or sets set package deprecation info.
        /// </summary>
        public DeprecationInfo? DeprecationInfo { get; set; }

        /// <summary>
        /// Gets whether or not the package is currently published.
        /// </summary>
        public bool IsUnlisted
        {
            get
            {
                return Published == Constants.Unpublished || Published == Constants.V2Unpublished;
            }
        }

        /// <summary>
        /// Gets whether or not the package is marked as pre-release.
        /// </summary>
        public bool IsPrerelease
        {
            get
            {
                return SemanticVersion != null && SemanticVersion.IsPrerelease;
            }
        }
    }
}
