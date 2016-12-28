using System.ComponentModel.DataAnnotations;
using System.Xml.Serialization;
using NuGetPe.Resources;

namespace NuGetPe
{
    [XmlType("dependency")]
    public class ManifestDependency
    {
        [Required(ErrorMessageResourceType = typeof(NuGetResources),
            ErrorMessageResourceName = "Manifest_DependencyIdRequired")]
        [XmlAttribute("id")]
        public string Id { get; set; }

        [XmlAttribute("version")]
        public string Version { get; set; }

        [XmlAttribute("exclude")]
        public string Exclude { get; set; }
    }
}