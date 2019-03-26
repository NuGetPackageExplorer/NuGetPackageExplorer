using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.ComponentModel;
using System.ComponentModel.Composition;
using System.Configuration;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using Newtonsoft.Json;
using NuGetPackageExplorer.Types;
using NuGetPe;
using OSVersionHelper;
using PackageExplorer.Properties;
using Windows.Storage;

namespace PackageExplorer
{
    [Export(typeof(ISettingsManager))]
    internal class SettingsManager : ISettingsManager, INotifyPropertyChanged
    {
        public const string ApiKeysSectionName = "apikeys";

        public event PropertyChangedEventHandler PropertyChanged;

        private void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        private T GetValue<T>([CallerMemberName] string? name = null)
        {
            object value;
            try
            {
                if (WindowsVersionHelper.HasPackageIdentity)
                {
                    value = GetValueFromLocalSettings<T>(name!)!;
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
            }
            catch (ConfigurationErrorsException)
            {
                // Corrupt settings file
                Settings.Default.Reset();

                // Try getting it again
                value = Settings.Default[name];
                if (typeof(T) == typeof(List<string>) && value is StringCollection sc)
                {
                    value = sc.Cast<string>().ToArray();
                }

                if (value is T t)
                {
                    return t;
                }
            }
            catch (UnauthorizedAccessException)
            { }
            catch (IOException)
            {
                // not much we can do if we can't read/write the settings file
            }

            return default!;
        }

        // Don't load these types inline
        [MethodImpl(MethodImplOptions.NoInlining)]
        private static object? GetValueFromLocalSettings<T>(string name)
        {
            object value;
            var settings = ApplicationData.Current.LocalSettings;
            value = settings.Values[name];
            if (typeof(T) == typeof(List<string>) && value is string str)
            {
                value = JsonConvert.DeserializeObject<List<string>>(str);
            }

            return value;
        }

        private void SetValue(object? value, string? name = null, [CallerMemberName] string? propertyName = null)
        {
            name ??= propertyName;

            try
            {
                if (WindowsVersionHelper.HasPackageIdentity)
                {
                    SetValueInLocalSettings(value, name!);
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
            }
            catch (UnauthorizedAccessException)
            { }
            catch (IOException)
            {
                // not much we can do if we can't read/write the settings file
            }

            OnPropertyChanged(propertyName);
        }

        // Don't load these types inline
        [MethodImpl(MethodImplOptions.NoInlining)]
        private static void SetValueInLocalSettings(object? value, string name)
        {
            var settings = ApplicationData.Current.LocalSettings;
            if (value is List<string> list)
            {
                value = JsonConvert.SerializeObject(list);
            }
            settings.Values[name] = value;
        }

        public IList<string> GetMruFiles()
        {
            return GetValue<List<string>>("MruFiles") ?? new List<string>();
        }

        public void SetMruFiles(IEnumerable<string> files)
        {
            SetValue(files.ToList(), "MruFiles");
        }

        public IList<string> GetPackageSources()
        {
            return GetValue<List<string>>("MruPackageSources") ?? new List<string>();
        }

        public void SetPackageSources(IEnumerable<string> sources)
        {
            SetValue(sources.ToList(), "MruPackageSources");
        }

        public string ActivePackageSource
        {
            get => GetValue<string>("PackageSource") ?? NuGetConstants.DefaultFeedUrl;
            set => SetValue(value, "PackageSource");
        }

        public IList<string> GetPublishSources()
        {
            return GetValue<List<string>>("PublishPackageSources") ?? new List<string>();
        }

        public void SetPublishSources(IEnumerable<string> sources)
        {
            SetValue(sources.ToList(), "PublishPackageSources");
        }

        public string ActivePublishSource
        {
            get => GetValue<string>("PublishPackageLocation") ?? NuGetConstants.NuGetPublishFeed;
            set => SetValue(value, "PublishPackageLocation");
        }

        public string? ReadApiKey(string source)
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
            get => GetValue<bool?>() ?? true;
            set => SetValue(value);
        }

        public bool AutoLoadPackages
        {
            get => GetValue<bool?>() ?? true;
            set => SetValue(value);
        }

        public bool PublishAsUnlisted
        {
            get => GetValue<bool?>() ?? false;
            set => SetValue(value);
        }

        public string SigningCertificate
        {
            get => GetValue<string>();
            set => SetValue(value);
        }

        public string? TimestampServer
        {
            get => GetValue<string>();
            set => SetValue(value);
        }

        public string SigningHashAlgorithmName
        {
            get => GetValue<string>();
            set => SetValue(value);
        }

        public int FontSize
        {
            get => GetValue<int?>() ?? 12;
            set => SetValue(value);
        }

        public bool ShowTaskShortcuts
        {
            get => GetValue<bool?>() ?? true;
            set => SetValue(value);
        }

        public string WindowPlacement
        {
            get => GetValue<string>();
            set => SetValue(value);
        }

        public double PackageChooserDialogWidth
        {
            get => GetValue<double?>() ?? 630;
            set => SetValue(value);
        }

        public double PackageChooserDialogHeight
        {
            get => GetValue<double?>() ?? 450;
            set => SetValue(value);
        }

        public double PackageContentHeight
        {
            get => GetValue<double?>() ?? 400;
            set => SetValue(value);
        }

        public double ContentViewerHeight
        {
            get => GetValue<double?>() ?? 400;
            set => SetValue(value);
        }

        public bool WordWrap
        {
            get => GetValue<bool?>() ?? false;
            set => SetValue(value);
        }

        public bool ShowLineNumbers
        {
            get => GetValue<bool?>() ?? false;
            set => SetValue(value);
        }
    }
}
