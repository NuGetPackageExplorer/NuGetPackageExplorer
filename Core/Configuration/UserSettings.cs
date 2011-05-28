using System;
using System.Collections.Generic;
using System.IO;
using System.Xml.Linq;
//using Microsoft.Internal.Web.Utils;
using NuGet.Resources;
using System.Globalization;

namespace NuGet {
    public class UserSettings : ISettings {
        private XDocument _config;
        private string _configLocation;
        private readonly IFileSystem _fileSystem;

        public UserSettings(IFileSystem fileSystem) {
            if (fileSystem == null) {
                throw new ArgumentNullException("fileSystem");
            }
            _fileSystem = fileSystem;
            _configLocation = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "NuGet", "NuGet.Config");
            _config = XmlUtility.GetOrCreateDocument("configuration", _fileSystem, _configLocation);
        }

        public string GetValue(string section, string key) {
            if (String.IsNullOrEmpty(section)) {
                throw new ArgumentException("Argument cannot be null or empty.", "section");
            }

            if (String.IsNullOrEmpty(key)) {
                throw new ArgumentException("Argument cannot be null or empty.", "key");
            }

            var kvps = GetValues(section);
            string value;
            if (kvps == null || !kvps.TryGetValue(key, out value)) {
                return null;
            }
            return value;
        }

        public IDictionary<string, string> GetValues(string section) {
            if (String.IsNullOrEmpty(section)) {
                throw new ArgumentException("Argument cannot be null or empty.", "section");
            }

            try {
                var sectionElement = _config.Root.Element(section);
                if (sectionElement == null) {
                    return null;
                }

                var kvps = new Dictionary<string, string>();
                foreach (var e in sectionElement.Elements("add")) {
                    var key = e.GetOptionalAttributeValue("key");
                    var value = e.GetOptionalAttributeValue("value");
                    if (!String.IsNullOrEmpty(key) && value != null) {
                        kvps.Add(key, value);
                    }
                }

                return kvps;
            }
            catch (Exception e) {
                throw new InvalidOperationException(NuGetResources.UserSettings_UnableToParseConfigFile, e);
            }
        }

        public void SetValue(string section, string key, string value) {
            if (String.IsNullOrEmpty(section)) {
                throw new ArgumentException("Argument cannot be null or empty.", "section");
            }
            if (String.IsNullOrEmpty(key)) {
                throw new ArgumentException("Argument cannot be null or empty.", "key");
            }
            if (value == null) {
                throw new ArgumentNullException("value");
            }

            var sectionElement = _config.Root.Element(section);
            if (sectionElement == null) {
                sectionElement = new XElement(section);
                _config.Root.Add(sectionElement);
            }

            foreach (var e in sectionElement.Elements("add")) {
                var tempKey = e.GetOptionalAttributeValue("key");

                if (tempKey == key) {
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

        public void DeleteValue(string section, string key) {
            if (String.IsNullOrEmpty(section)) {
                throw new ArgumentException("Argument cannot be null or empty.", "section");
            }
            if (String.IsNullOrEmpty(key)) {
                throw new ArgumentException("Argument cannot be null or empty.", "key");
            }

            var sectionElement = _config.Root.Element(section);
            if (sectionElement == null) {
                throw new InvalidOperationException(String.Format(CultureInfo.CurrentCulture, NuGetResources.UserSettings_SectionDoesNotExist, section));
            }

            XElement elementToDelete = null;
            foreach (var e in sectionElement.Elements("add")) {
                if (e.GetOptionalAttributeValue("key") == key) {
                    elementToDelete = e;
                    break;
                }
            }
            if (elementToDelete == null) {
                throw new InvalidOperationException(String.Format(CultureInfo.CurrentCulture, NuGetResources.UserSettings_SectionDoesNotExist, section));
            }
            elementToDelete.Remove();
            Save(_config);

        }

        private void Save(XDocument document) {
            _fileSystem.AddFile(_configLocation, document.Save);
        }
    }
}
