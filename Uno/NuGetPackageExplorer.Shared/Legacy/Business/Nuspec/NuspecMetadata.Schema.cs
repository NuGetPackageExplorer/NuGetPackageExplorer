using System;
using System.Collections.Generic;
using System.Text;
using System.Xml.Serialization;

namespace NupkgExplorer.Business.Nuspec
{
	// https://docs.microsoft.com/en-us/nuget/reference/nuspec
	// https://github.com/NuGet/NuGet.Client/blob/dev/src/NuGet.Core/NuGet.Packaging/compiler/resources/nuspec.xsd

	public partial class NuspecMetadata
	{
		public string Id { get; set; }
		public string Version { get; set; }
		public string Title { get; set; }
		public string Authors { get; set; }
		public string Owners { get; set; }
		public Uri LicenseUrl { get; set; }
		public string ProjectUrl { get; set; }
		public string IconUrl { get; set; }
		public bool RequireLicenseAcceptance { get; set; }
		public bool DevelopmentDependency { get; set; }
		public string Description { get; set; }
		public string Summary { get; set; }
		public string ReleaseNotes { get; set; }
		public string Copyright { get; set; }
		public string Language { get; set; }
		public string Tags { get; set; }
		public bool Serviceable { get; set; }
		public string Icon { get; set; }
		[XmlElement] public NuspecRepository Repository { get; set; }
		[XmlElement] public NuspecLicense License { get; set; }
		public NuspecPackageType[] PackageTypes { get; set; }
		[XmlArrayItem("dependency", typeof(NuspecDependency))]
		[XmlArrayItem("group", typeof(NuspecDependencyGroup))]
		public INuspecDependency[] Dependencies { get; set; }
		public NuspecFrameworkAssembly[] FrameworkAssemblies { get; set; }
		[XmlArrayItem("group", typeof(NuspecFrameworkReferenceGroup))]
		public NuspecFrameworkReferenceGroup[] FrameworkReferences { get; set; }
		[XmlArrayItem("reference", typeof(NuspecReference))]
		[XmlArrayItem("group", typeof(NuspecReferenceGroup))]
		public INuspecReference[] References { get; set; }
		public NuspecContentFile[] ContentFiles { get; set; }
		public string MinClientVersion { get; set; }
	}
	public class NuspecRepository
	{
		public string Type { get; set; }
		public Uri Url { get; set; }
		public string Commit { get; set; }
	}
	public class NuspecLicense
	{
		public string Type { get; set; }
		public string Version { get; set; }

		[XmlText] public string Value { get; set; }
	}
	public class NuspecPackageType
	{
		public string Name { get; set; }
		public string Version { get; set; }
	}
	public interface INuspecDependency { }
	public class NuspecDependency : INuspecDependency
	{
		public string Id { get; set; }
		public string Version { get; set; }
		public string Include { get; set; }
		public string Exclude { get; set; }
	}
	public class NuspecDependencyGroup : INuspecDependency
	{
		public string TargetFramework { get; set; }
		[XmlElement("dependency")]
		public NuspecDependency[] Dependencies { get; set; }
	}
	public class NuspecFrameworkAssembly
	{
		public string AssemblyName { get; set; }
		public string TargetFramework { get; set; }
	}
	public class NuspecFrameworkReferenceGroup
	{
		public string TargetFramework { get; set; }
		[XmlElement("frameworkReference")]
		public NuspecFrameworkReference[] Dependencies { get; set; }
	}
	public class NuspecFrameworkReference
	{
		public string Name { get; set; }
	}
	public interface INuspecReference { }
	public class NuspecReference : INuspecReference
	{
		public string File { get; set; }
	}
	public class NuspecReferenceGroup : INuspecReference
	{
		public string TargetFramework { get; set; }
		[XmlElement("reference")]
		public NuspecReference[] References { get; set; }
	}
	public class NuspecContentFile
	{
		public string Include { get; set; }
		public string Exclude { get; set; }
		public string BuildAction { get; set; }
		public bool CopyToOutput { get; set; }
		public bool Flatten { get; set; }
	}
}
