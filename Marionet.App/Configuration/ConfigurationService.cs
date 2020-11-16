using System;
using System.IO;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace Marionet.App.Configuration
{
    public class ConfigurationService : IDisposable
    {
        private const int SettingsWatcherTimeout = 100;

        public static readonly string ConfigurationDirectory = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Marionet");
        public static readonly string ConfigurationFile = Path.Combine(ConfigurationDirectory, "config.json");

        public static readonly Config DefaultConfiguration = new Config();

        private readonly SemaphoreSlim storageLock = new SemaphoreSlim(1, 1);
        private readonly JsonSerializerOptions jsonSerializerOptions;
        private readonly FileSystemWatcher settingsFileWatcher;
        private CancellationTokenSource? reloadEventCancellation;
        private bool disposedValue;

        public ConfigurationService()
        {
            if (!Directory.Exists(ConfigurationDirectory))
            {
                Directory.CreateDirectory(ConfigurationDirectory);
            }

            jsonSerializerOptions = new JsonSerializerOptions() { WriteIndented = true, PropertyNamingPolicy = JsonNamingPolicy.CamelCase };

            settingsFileWatcher = new FileSystemWatcher(ConfigurationDirectory)
            {
                NotifyFilter = NotifyFilters.LastWrite,
                Filter = "*.json"
            };
            settingsFileWatcher.Changed += OnSettingsFileChanged;
            settingsFileWatcher.EnableRaisingEvents = true;

            DesktopManagement = new DesktopManagement(this);
            CertificateManagement = new CertificateManagement(this);
        }

        public Config Configuration { get; private set; } = DefaultConfiguration;

        internal DesktopManagement DesktopManagement { get; }

        internal CertificateManagement CertificateManagement { get; }

        public event EventHandler? ConfigurationChanged;

        public async Task Save()
        {
            await storageLock.WaitAsync();
            await File.WriteAllTextAsync(ConfigurationFile, JsonSerializer.Serialize(Configuration, jsonSerializerOptions));
            storageLock.Release();
        }

        public async Task Load()
        {
            await storageLock.WaitAsync();
            if (!File.Exists(ConfigurationFile))
            {
                File.WriteAllText(ConfigurationFile, JsonSerializer.Serialize(DefaultConfiguration, jsonSerializerOptions));
            }

            Configuration = JsonSerializer.Deserialize<Config>(File.ReadAllText(ConfigurationFile), jsonSerializerOptions) ?? new Config();
            storageLock.Release();
        }

        public async Task Update(Config config)
        {
            Configuration = config;
            await Save();
        }

        private async void OnSettingsFileChanged(object sender, FileSystemEventArgs e)
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
                    ConfigurationChanged?.Invoke(null, new EventArgs());
                }
                catch (TaskCanceledException) { }
            }
        }

        protected virtual void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                if (disposing)
                {
                    storageLock.Dispose();
                    settingsFileWatcher.EnableRaisingEvents = false;
                    settingsFileWatcher.Changed -= OnSettingsFileChanged;
                    settingsFileWatcher.Dispose();
                    reloadEventCancellation?.Dispose();
                    CertificateManagement.Dispose();
                }

                disposedValue = true;
            }
        }

        public void Dispose()
        {
            Dispose(disposing: true);
            GC.SuppressFinalize(this);
        }
    }
}
