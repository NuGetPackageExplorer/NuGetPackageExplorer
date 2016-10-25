﻿using System.ComponentModel.DataAnnotations;
using System.Xml.Serialization;
using NuGetPe.Resources;

namespace NuGetPe
{
    [XmlType("frameworkAssembly")]
    public class ManifestFrameworkAssembly
    {
        [Required(ErrorMessageResourceType = typeof(NuGetResources),
            ErrorMessageResourceName = "Manifest_AssemblyNameRequired")]
        [XmlAttribute("assemblyName")]
        public string AssemblyName { get; set; }

        [XmlAttribute("targetFramework")]
        public string TargetFramework { get; set; }
    }
}