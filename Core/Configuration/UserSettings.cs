using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Xml.Linq;
using NuGetPe.Resources;

//using Microsoft.Internal.Web.Utils;

namespace NuGetPe
{
    public class UserSettings : ISettings
    {
        private readonly XDocument _config;
        private readonly string _configLocation;
        private readonly IFileSystem _fileSystem;

        public UserSettings(IFileSystem fileSystem)
        {
            _fileSystem = fileSystem ?? throw new ArgumentNullException("fileSystem");
            _configLocation = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "NuGet",
                                           "NuGet.Config");
            _config = XmlUtility.GetOrCreateDocument("configuration", _fileSystem, _configLocation);
        }

        public string? GetValue(string section, string key)
        {
            if (string.IsNullOrEmpty(section))
            {
                throw new ArgumentException("Argument cannot be null or empty.", "section");
            }

            if (string.IsNullOrEmpty(key))
            {
                throw new ArgumentException("Argument cannot be null or empty.", "key");
            }

            var kvps = GetValues(section);
            if (kvps == null || !kvps.TryGetValue(key, out var value))
            {
                return null;
            }
            return value;
        }

        public IDictionary<string, string>? GetValues(string section)
        {
            if (string.IsNullOrEmpty(section))
            {
                throw new ArgumentException("Argument cannot be null or empty.", "section");
            }

            try
            {
                var sectionElement = _config.Root.Element(section);
                if (sectionElement == null)
                {
                    return null;
                }

                var kvps = new Dictionary<string, string>();
                foreach (var e in sectionElement.Elements("add"))
                {
                    var key = e.GetOptionalAttributeValue("key");
                    var value = e.GetOptionalAttributeValue("value");
                    if (!string.IsNullOrEmpty(key) && value != null)
                    {
                        kvps.Add(key, value);
                    }
                }

                return kvps;
            }
            catch (Exception e)
            {
                DiagnosticsClient.TrackException(e);
                throw new InvalidOperationException(NuGetResources.UserSettings_UnableToParseConfigFile, e);
            }
        }

        public void SetValue(string section, string key, string value)
        {
            if (string.IsNullOrEmpty(section))
            {
                throw new ArgumentException("Argument cannot be null or empty.", "section");
            }
            if (string.IsNullOrEmpty(key))
            {
                throw new ArgumentException("Argument cannot be null or empty.", "key");
            }
            if (value == null)
            {
                throw new ArgumentNullException("value");
            }

            var sectionElement = _config.Root.Element(section);
            if (sectionElement == null)
            {
                sectionElement = new XElement(section);
                _config.Root.Add(sectionElement);
            }

            foreach (var e in sectionElement.Elements("add"))
            {
                var tempKey = e.GetOptionalAttributeValue("key");

                if (tempKey == key)
                {
                    e.SetAttributeValue("value", value);
                    Save(_config);
                    return;
                }
            }

            var addElement = new XElement("add");
            addElement.SetAttributeValue("key", key);
            addElement.SetAttributeValue("value", value);
            sectionElement.Add(addElement);
            Save(_config);
        }

        public void DeleteValue(string section, string key)
        {
            if (string.IsNullOrEmpty(section))
            {
                throw new ArgumentException("Argument cannot be null or empty.", "section");
            }
            if (string.IsNullOrEmpty(key))
            {
                throw new ArgumentException("Argument cannot be null or empty.", "key");
            }

            var sectionElement = _config.Root.Element(section);
            if (sectionElement == null)
            {
                throw new InvalidOperationException(string.Format(CultureInfo.CurrentCulture,
                                                                  NuGetResources.UserSettings_SectionDoesNotExist,
                                                                  section));
            }

            XElement? elementToDelete = null;
            foreach (var e in sectionElement.Elements("add"))
            {
                if (e.GetOptionalAttributeValue("key") == key)
                {
                    elementToDelete = e;
                    break;
                }
            }
            if (elementToDelete == null)
            {
                throw new InvalidOperationException(string.Format(CultureInfo.CurrentCulture,
                                                                  NuGetResources.UserSettings_SectionDoesNotExist,
                                                                  section));
            }
            elementToDelete.Remove();
            Save(_config);
        }

        private void Save(XDocument document)
        {
            _fileSystem.AddFile(_configLocation, document.Save);
        }
    }
}
