using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Xml;
using System.Xml.Linq;
using System.Xml.XPath;
using NupkgExplorer.Framework.Xml;

namespace NupkgExplorer.Business.Nuspec
{
	public partial class NuspecMetadata
	{
		public static NuspecMetadata Parse(XDocument document)
		{
			var metadata =
				document.XPathSelectElement("//nuspec:package/nuspec:metadata", GetResolver("http://schemas.microsoft.com/packaging/2013/05/nuspec.xsd")) ??
				document.XPathSelectElement("//nuspec:package/nuspec:metadata", GetResolver("http://schemas.microsoft.com/packaging/2011/08/nuspec.xsd")) ??
				document.XPathSelectElement("//package/metadata") ??
				document.Descendants().FirstOrDefault(x => x.Name.LocalName == "metadata");
			if (metadata == null)
			{
				throw new Exception("Cannot find <metadata> element within nuspec document.");
			}

			return metadata.DeserializeObject<NuspecMetadata>();

			XmlNamespaceManager GetResolver(string uri)
			{
				var resolver = new XmlNamespaceManager(new NameTable());
				resolver.AddNamespace("nuspec", uri);

				return resolver;
			}
		}
	}
}
