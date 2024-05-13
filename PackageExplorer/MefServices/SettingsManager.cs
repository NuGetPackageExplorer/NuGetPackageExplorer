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
using Windows.Storage;
using NuGet.Configuration;
using NpeConstants = NuGetPe.NuGetConstants;

#if !HAS_UNO && !USE_WINUI
using OSVersionHelper;
using NpeSettings = PackageExplorer.Properties.Settings;
#endif

namespace PackageExplorer
{
    [Export(typeof(ISettingsManager))]
    internal class SettingsManager : ISettingsManager, INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler? PropertyChanged;

        private readonly object _lockObject = new object();

        private void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        private T GetValue<T>([CallerMemberName] string? name = null)
        {
            lock (_lockObject)
            {
                object value;
                try
                {
#if !HAS_UNO && !USE_WINUI
                    if (WindowsVersionHelper.HasPackageIdentity)
                    {
                        value = GetValueFromLocalSettings<T>(name!)!;
                    }
                    else
                    {
                        value = NpeSettings.Default[name];
                        if (typeof(T) == typeof(List<string>) && value is StringCollection sc)
                        {
                            value = sc.Cast<string>().ToList();
                        }
                    }
#else
                    Windows.Storage.ApplicationData.Current.LocalSettings.Values.TryGetValue(name, out value!);
#endif

                    if (value is T t)
                    {
                        return t;
                    }
                }
#if !HAS_UNO && !USE_WINUI
                catch (ConfigurationErrorsException)
                {
                    // Corrupt settings file
                    NpeSettings.Default.Reset();

                    // Try getting it again
                    value = NpeSettings.Default[name];
                    if (typeof(T) == typeof(List<string>) && value is StringCollection sc)
                    {
                        value = sc.Cast<string>().ToList();
                    }

                    if (value is T t)
                    {
                        return t;
                    }
                }
#endif
                catch (UnauthorizedAccessException)
                { }
                catch (IOException)
                {
                    // not much we can do if we can't read/write the settings file
                }

                return default!;
            }
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

            lock (_lockObject)
            {
                try
                {
#if !HAS_UNO && !USE_WINUI
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
                        NpeSettings.Default[name] = value;
                    }
#else
                    Windows.Storage.ApplicationData.Current.LocalSettings.Values[name] = value;
#endif
                }
                catch (UnauthorizedAccessException)
                { }
                catch (IOException)
                {
                    // not much we can do if we can't read/write the settings file
                }
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
            var list = new List<string>();
            var nugetSettings = Settings.LoadDefaultSettings(null);
            foreach (var packageSource in SettingsUtility.GetEnabledSources(nugetSettings))
            {
                list.Add(packageSource.Source);
            }

            var npeSources = GetValue<List<string>>("MruPackageSources");
            if (npeSources != null)
            {
                foreach (var npeSource in npeSources)
                {
                    if (!list.Contains(npeSource))
                    {
                        list.Add(npeSource);
                    }
                }
            }

            return list;
        }

        public void SetPackageSources(IEnumerable<string> sources)
        {
            SetValue(sources.ToList(), "MruPackageSources");
        }

        public string ActivePackageSource
        {
            get => GetValue<string>("PackageSource") ?? NpeConstants.DefaultFeedUrl;
            set => SetValue(value, "PackageSource");
        }

        public IList<string> GetPublishSources()
        {
            var list = new List<string>();
            var nugetSettings = Settings.LoadDefaultSettings(null);
            foreach (var packageSource in SettingsUtility.GetEnabledSources(nugetSettings))
            {
                list.Add(packageSource.Source);
            }

            var npeSources = GetValue<List<string>>("PublishPackageSources");
            if (npeSources != null)
            {
                foreach (var npeSource in npeSources)
                {
                    if (!list.Contains(npeSource))
                    {
                        list.Add(npeSource);
                    }
                }
            }

            return list;
        }

        public void SetPublishSources(IEnumerable<string> sources)
        {
            SetValue(sources.ToList(), "PublishPackageSources");
        }

        public string ActivePublishSource
        {
            get
            {
                var publishSource = GetValue<string>("PublishPackageLocation");
                if (publishSource == null)
                {
                    var nugetSettings = Settings.LoadDefaultSettings(null);
                    publishSource = SettingsUtility.GetDefaultPushSource(nugetSettings);
                }
                return publishSource ?? NpeConstants.NuGetPublishFeed;
            }
            set => SetValue(value, "PublishPackageLocation");
        }

        public string? ReadApiKey(string source)
        {
            var nugetSettings = Settings.LoadDefaultSettings(null);
            return SettingsUtility.GetDecryptedValueForAddItem(nugetSettings, ConfigurationConstants.ApiKeys, source);
        }

        public void WriteApiKey(string source, string apiKey)
        {
            var nugetSettings = Settings.LoadDefaultSettings(null);
            SettingsUtility.SetEncryptedValueForAddItem(nugetSettings, ConfigurationConstants.ApiKeys, source, apiKey);
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
