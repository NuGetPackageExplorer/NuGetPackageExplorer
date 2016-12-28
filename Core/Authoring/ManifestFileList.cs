using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Xml.Serialization;

namespace NuGetPe
{
    /// <summary>
    /// This class is used to work around the bug (or bad design, depending on how you look at it) in .NET XML 
    /// deserialization engine in that it always deserializes a missing collection element as an empty list, 
    /// instead of null.
    /// </summary>
    public sealed class ManifestFileList
    {
        [SuppressMessage("Microsoft.Design", "CA1002:DoNotExposeGenericLists",
            Justification = "It's easier to create a list")]
        [SuppressMessage("Microsoft.Usage", "CA2227:CollectionPropertiesShouldBeReadOnly",
            Justification = "This is needed for xml serialization")]
        [XmlElement("file", IsNullable = false)]
        public List<ManifestFile> Items { get; set; }
    }
}