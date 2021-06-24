using System;
using System.Collections.Generic;
using System.Text;

namespace NupkgExplorer.Client.Data
{
	public class PackageData
	{
		public string Id { get; set; }
		public string Version { get; set; }
		public string Description { get; set; }
		public PackageVersion[] Versions { get; set; }
		public string[] Authors { get; set; }
		public string IconUrl { get; set; }
		public string LicenseUrl { get; set; }
		public string[] Owners { get; set; }
		public string ProjectUrl { get; set; }
		public string Registration { get; set; }
		public string Summary { get; set; }
		public string[] Tags { get; set; }
		public string Title { get; set; }
		public long TotalDownloads { get; set; }
		public bool Verified { get; set; }
		public PackageType[] PackageTypes { get; set; }
	}
}
