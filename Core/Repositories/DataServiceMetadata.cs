using System;
using System.Collections.Generic;
using System.Data.Services.Client;
using System.Linq;
using System.Text;
using System.Xml.Linq;

namespace NuGet
{
    internal static class DataServiceMetadata
    {
        public static ISet<string> GetDataServiceMetadata(this DataServiceContext context)
        {
            Uri metadataUri = context.GetMetadataUri();

            if (metadataUri == null)
            {
                return null;
            }

            // Make a request to the metadata uri and get the schema
            var client = new HttpClient(metadataUri);
            byte[] data = client.DownloadData();
            if (data == null)
            {
                return null;
            }

            string schema = Encoding.UTF8.GetString(data);

            return ExtractMetadataFromSchema(schema);
        }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Design", "CA1031:DoNotCatchGeneralExceptionTypes")]
        internal static ISet<string> ExtractMetadataFromSchema(string schema)
        {
            if (String.IsNullOrEmpty(schema))
            {
                return null;
            }

            XDocument schemaDocument;

            try
            {
                schemaDocument = XDocument.Parse(schema);
            }
            catch (Exception)
            {
                // If the schema is malformed (for some reason) then just return empty list
                return null;
            }

            return ExtractMetadataInternal(schemaDocument);
        }

        private static ISet<string> ExtractMetadataInternal(XDocument schemaDocument)
        {
            // Get all entity containers
            var entityContainers = from e in schemaDocument.Descendants()
                                   where e.Name.LocalName == "EntityContainer"
                                   select e;

            // Find the entity container with the Packages entity set
            var result = (from e in entityContainers
                          let entitySet = e.Elements().FirstOrDefault(el => el.Name.LocalName == "EntitySet")
                          let name = entitySet != null ? entitySet.Attribute("Name").Value : null
                          where name != null && name.Equals("Packages", StringComparison.OrdinalIgnoreCase)
                          select new { Container = e, EntitySet = entitySet }).FirstOrDefault();

            if (result == null)
            {
                return null;
            }

            var packageEntityTypeAttribute = result.EntitySet.Attribute("EntityType");
            string packageEntityName = null;
            if (packageEntityTypeAttribute != null)
            {
                packageEntityName = packageEntityTypeAttribute.Value;
            }

            return new HashSet<string>(ExtractSupportedProperties(schemaDocument, packageEntityName), StringComparer.OrdinalIgnoreCase);
        }

        private static IEnumerable<string> ExtractSupportedProperties(XDocument schemaDocument, string packageEntityName)
        {
            // The name is listed in the entity set listing as <EntitySet Name="Packages" EntityType="Gallery.Infrastructure.FeedModels.PublishedPackage" />
            // We need to extract the name portion to look up the entity type <EntityType Name="PublishedPackage" 
            packageEntityName = TrimNamespace(packageEntityName);

            var packageEntity = (from e in schemaDocument.Descendants()
                                 where e.Name.LocalName == "EntityType"
                                 let attribute = e.Attribute("Name")
                                 where attribute != null && attribute.Value.Equals(packageEntityName, StringComparison.OrdinalIgnoreCase)
                                 select e).FirstOrDefault();

            if (packageEntity != null)
            {
                return from e in packageEntity.Elements()
                       where e.Name.LocalName == "Property"
                       select e.Attribute("Name").Value;
            }
            return Enumerable.Empty<string>();
        }

        private static string TrimNamespace(string packageEntityName)
        {
            int lastIndex = packageEntityName.LastIndexOf('.');
            if (lastIndex > 0 && lastIndex < packageEntityName.Length)
            {
                packageEntityName = packageEntityName.Substring(lastIndex + 1);
            }
            return packageEntityName;
        }
    }
}