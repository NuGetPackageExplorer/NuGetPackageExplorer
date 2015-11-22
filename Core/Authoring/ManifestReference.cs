using System.ComponentModel.DataAnnotations;
using System.Xml.Serialization;
using NuGet.Resources;

namespace NuGet
{
    [XmlType("reference")]
    public class ManifestReference
    {
        [Required(ErrorMessageResourceType = typeof(NuGetResources),
            ErrorMessageResourceName = "Manifest_RequiredMetadataMissing")]
        [XmlAttribute("file")]
        public string File { get; set; }
    }
}