using System.Collections.Generic;

namespace NuGetPe
{
    public interface ISettings
    {
        string? GetValue(string section, string key);
        IDictionary<string, string>? GetValues(string section);
        void SetValue(string section, string key, string value);
        void DeleteValue(string section, string key);
    }
}
