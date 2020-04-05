using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace Marionet.App.Configuration
{
    public class Config
    {
        public const int ServerPort = 23549;
        public const int SettingsWatcherTimeout = 100;

        public static readonly string ConfigurationDirectory = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Marionet");
        public static readonly string ConfigurationFile = Path.Combine(ConfigurationDirectory, "config.json");

        private static readonly SemaphoreSlim storageLock = new SemaphoreSlim(1, 1);
        private static readonly JsonSerializerOptions jsonSerializerOptions = new JsonSerializerOptions() { WriteIndented = true, PropertyNamingPolicy = JsonNamingPolicy.CamelCase };
        private static readonly FileSystemWatcher settingsWatcher;
        private static CancellationTokenSource? reloadEventCancellation;

        static Config()
        {
            if (!Directory.Exists(ConfigurationDirectory))
            {
                Directory.CreateDirectory(ConfigurationDirectory);
            }
            settingsWatcher = new FileSystemWatcher(ConfigurationDirectory);
            settingsWatcher.NotifyFilter = NotifyFilters.LastWrite;
            settingsWatcher.Filter = "*.json";
            settingsWatcher.Changed += SettingsFileChanged;
            settingsWatcher.EnableRaisingEvents = true;
        }

        public static event EventHandler? SettingsReloaded;

        private static async void SettingsFileChanged(object sender, FileSystemEventArgs e)
        {
            if (e.FullPath == ConfigurationFile)
            {
                if (reloadEventCancellation != null)
                {
                    reloadEventCancellation.Cancel();
                    reloadEventCancellation.Dispose();
                }

                reloadEventCancellation = new CancellationTokenSource();
                try
                {
                    await Task.Delay(SettingsWatcherTimeout, reloadEventCancellation.Token);
                    await Load();
                    SettingsReloaded?.Invoke(null, new EventArgs());
                }
                catch (TaskCanceledException) { }
            }
        }

        public static async Task Save()
        {
            await storageLock.WaitAsync();
            await File.WriteAllTextAsync(ConfigurationFile, JsonSerializer.Serialize(Instance, jsonSerializerOptions));
            storageLock.Release();
        }

        public static async Task Load()
        {
            await storageLock.WaitAsync();
            if (!File.Exists(ConfigurationFile))
            {
                File.WriteAllText(ConfigurationFile, JsonSerializer.Serialize(Instance, jsonSerializerOptions));
            }
            Instance = JsonSerializer.Deserialize<Config>(File.ReadAllText(ConfigurationFile), jsonSerializerOptions);
            storageLock.Release();
        }

        private Config() { }

        public static Config Instance { get; private set; } = new Config();

        public string ServerCertificatePath { get; set; } = Path.Combine(ConfigurationDirectory, "server.pfx");

        public string ClientCertificatePath { get; set; } = Path.Combine(ConfigurationDirectory, "client.pfx");

        public string Self { get; set; } = Environment.MachineName;


        [System.Diagnostics.CodeAnalysis.SuppressMessage("Usage", "CA2227:Collection properties should be read only", Justification = "Used for serialization.")]
        public List<string> Desktops { get; set; } = new List<string>() { Environment.MachineName };

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Usage", "CA2227:Collection properties should be read only", Justification = "Used for serialization.")]
        public Dictionary<string, string> DesktopAddresses { get; set; } = new Dictionary<string, string>()
        {
            { Environment.MachineName, Environment.MachineName }
        };

        public RunConditions RunConditions { get; set; } = new RunConditions();
    }
}
