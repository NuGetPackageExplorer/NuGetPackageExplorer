using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.ComponentModel;
using System.ComponentModel.Composition;
using System.Linq;
using System.Runtime.CompilerServices;
using Newtonsoft.Json;
using NuGetPackageExplorer.Types;
using NuGetPe;
using PackageExplorer.Properties;
using Windows.Storage;

namespace PackageExplorer
{
    [Export(typeof(ISettingsManager))]
    internal class SettingsManager : ISettingsManager, INotifyPropertyChanged
    {
        public const string ApiKeysSectionName = "apikeys";

        public event PropertyChangedEventHandler PropertyChanged;

        private void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        private T GetValue<T>(string name)
        {
            object value;

            if (NativeMethods.IsRunningAsUwp)
            {
                value = ApplicationData.Current.LocalSettings.Values[name];

                if (typeof(T) == typeof(List<string>) && value is string str)
                {
                    value = JsonConvert.DeserializeObject<List<string>>(str);
                }
            }
            else
            {
                value = Settings.Default[name];

                if (typeof(T) == typeof(List<string>) && value is StringCollection sc)
                {
                    value = sc.Cast<string>().ToArray();
                }
            }

            if (value is T t)
            {
                return t;
            }
            return default(T);
        }

        private void SetValue(string name, object value)
        {
            if (NativeMethods.IsRunningAsUwp)
            {
                if (value is List<string> list)
                {
                    value = JsonConvert.SerializeObject(list);
                }
                ApplicationData.Current.LocalSettings.Values[name] = value;
            }
            else
            {
                if (value is List<string> list)
                {
                    var sc = new StringCollection();
                    sc.AddRange(list.ToArray());
                    value = sc;
                }
                Settings.Default[name] = value;
            }
            OnPropertyChanged(name);
        }

        #region ISettingsManager Members

        public IList<string> GetMruFiles()
        {
            return GetValue<List<string>>("MruFiles") ?? new List<string>();
        }

        public void SetMruFiles(IEnumerable<string> files)
        {
            SetValue("MruFiles", files.ToList());
        }

        public IList<string> GetPackageSources()
        {
            return GetValue<List<string>>("MruPackageSources") ?? new List<string>();
        }

        public void SetPackageSources(IEnumerable<string> sources)
        {
            SetValue("MruPackageSources", sources.ToList());
        }

        public string ActivePackageSource
        {
            get => GetValue<string>("PackageSource") ?? "https://api.nuget.org/v3/index.json";
            set => SetValue("PackageSource", value);
        }

        public IList<string> GetPublishSources()
        {
            return GetValue<List<string>>("PublishPackageSources") ?? new List<string>();
        }

        public void SetPublishSources(IEnumerable<string> sources)
        {
            SetValue("PublishPackageSources", sources.ToList());
        }

        public string ActivePublishSource
        {
            get => GetValue<string>("PublishPackageLocation") ?? "https://nuget.org";
            set => SetValue("PublishPackageLocation", value);
        }

        public string ReadApiKey(string source)
        {
            var settings = new UserSettings(new PhysicalFileSystem(Environment.CurrentDirectory));
            var key = settings.GetDecryptedValue(ApiKeysSectionName, source);

            return key;
        }

        public void WriteApiKey(string source, string apiKey)
        {
            var settings = new UserSettings(new PhysicalFileSystem(Environment.CurrentDirectory));
            settings.SetEncryptedValue(ApiKeysSectionName, source, apiKey);
        }

        public bool ShowPrereleasePackages
        {
            get => GetValue<bool?>("ShowPrereleasePackages") ?? true;
            set => SetValue("ShowPrereleasePackages", value);
        }

        public bool AutoLoadPackages
        {
            get => GetValue<bool?>("AutoLoadPackages") ?? true;
            set => SetValue("AutoLoadPackages", value);
        }

        public bool PublishAsUnlisted
        {
            get => GetValue<bool?>("PublishAsUnlisted") ?? false;
            set => SetValue("PublishAsUnlisted", value);
        }

        public string SigningCertificate
        {
            get => GetValue<string>("SigningCertificate");
            set => SetValue("SigningCertificate", value);
        }

        public string TimestampServer
        {
            get => GetValue<string>("TimestampServer");
            set => SetValue("TimestampServer", value);
        }

        public string SigningHashAlgorithmName
        {
            get => GetValue<string>("SigningHashAlgorithmName");
            set => SetValue("SigningHashAlgorithmName", value);
        }

        public int FontSize
        {
            get => GetValue<int?>("FontSize") ?? 12;
            set => SetValue("FontSize", value);
        }

        public bool ShowTaskShortcuts
        {
            get => GetValue<bool?>("ShowTaskShortcuts") ?? true;
            set => SetValue("ShowTaskShortcuts", value);
        }

        public string WindowPlacement
        {
            get => GetValue<string>("WindowPlacement");
            set => SetValue("WindowPlacement", value);
        }

        public double PackageChooserDialogWidth
        {
            get => GetValue<double?>("PackageChooserDialogWidth") ?? 630;
            set => SetValue("PackageChooserDialogWidth", value);
        }

        public double PackageChooserDialogHeight
        {
            get => GetValue<double?>("PackageChooserDialogHeight") ?? 450;
            set => SetValue("PackageChooserDialogHeight", value);
        }

        public double PackageContentHeight
        {
            get => GetValue<double?>("PackageContentHeight") ?? 400;
            set => SetValue("PackageContentHeight", value);
        }

        public double ContentViewerHeight
        {
            get => GetValue<double?>("ContentViewerHeight") ?? 400;
            set => SetValue("ContentViewerHeight", value);
        }

        public bool WordWrap
        {
            get => GetValue<bool?>("WordWrap") ?? false;
            set => SetValue("WordWrap", value);
        }

        public bool ShowLineNumbers
        {
            get => GetValue<bool?>("ShowLineNumbers") ?? false;
            set => SetValue("ShowLineNumbers", value);
        }

        #endregion
    }
}
