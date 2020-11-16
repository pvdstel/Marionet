using System;
using System.Collections.Generic;
using System.IO;

namespace Marionet.App.Configuration
{
    public record Config : SynchronizedConfig
    {
        public const int ServerPort = 23549;

        public string ServerCertificatePath { get; init; } = Path.Combine(ConfigurationService.ConfigurationDirectory, "server.pfx");

        public string ClientCertificatePath { get; init; } = Path.Combine(ConfigurationService.ConfigurationDirectory, "client.pfx");

        public string Self { get; init; } = Environment.MachineName;

        public int StickyCornerSize { get; init; } = 6;

        public bool BlockTransferWhenButtonPressed { get; init; } = true;

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Usage", "CA2227:Collection properties should be read only", Justification = "It's an init-only set.")]
        public Dictionary<string, string> DesktopAddresses { get; init; } = new Dictionary<string, string>()
        {
            { Environment.MachineName, Environment.MachineName }
        };

        public RunConditions RunConditions { get; init; } = new RunConditions();

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
