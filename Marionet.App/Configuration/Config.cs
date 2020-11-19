using System;
using System.Collections.Immutable;
using System.IO;

namespace Marionet.App.Configuration
{
    public record Config : SynchronizedConfig
    {
        public const int ServerPort = 23549;

        public string ServerCertificatePath { get; init; } = Path.Combine(ConfigurationService.ConfigurationDirectory, "server.pfx");

        public string ClientCertificatePath { get; init; } = Path.Combine(ConfigurationService.ConfigurationDirectory, "client.pfx");

        public bool BlockTransferWhenButtonPressed { get; init; } = true;

        public bool ShowTrayIcon { get; init; } = true;

        public int StickyCornerSize { get; init; } = 6;

        public int MinTransferDistance { get; init; } = 10;

        public int MaxTransferDistance { get; init; } = 0;

        public RunConditions RunConditions { get; init; } = new RunConditions();

        public ImmutableDictionary<string, string> DesktopAddresses { get; init; } = ImmutableDictionary<string, string>.Empty.Add(Environment.MachineName, Environment.MachineName);

        public string Self { get; init; } = Environment.MachineName;

        public SynchronizedConfig ToSynchronizedConfig()
        {
            return new SynchronizedConfig()
            {
                Desktops = this.Desktops,
                DesktopYOffsets = this.DesktopYOffsets,
            };
        }

        public Config ApplySynchronizedConfig(SynchronizedConfig synchronizedConfig)
        {
            if (synchronizedConfig == null) throw new ArgumentNullException(nameof(synchronizedConfig));

            return new Config(this)
            {
                Desktops = synchronizedConfig.Desktops,
                DesktopYOffsets = synchronizedConfig.DesktopYOffsets,
            };
        }
    }
}
