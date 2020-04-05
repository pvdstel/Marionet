using Marionet.Core;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Marionet.App.Configuration
{
    internal static class Desktop
    {
        private static List<string>? lastDesktops = new List<string>();

        static Desktop()
        {
            Config.SettingsReloaded += (s, e) =>
            {
                if (!Config.Instance.Desktops.SequenceEqual(lastDesktops))
                {
                    lastDesktops = Config.Instance.Desktops.ToList();
                    DesktopsChanged?.Invoke(null, new EventArgs());
                }
            };
        }

        public static event EventHandler? DesktopsChanged;

        public static async Task AddFromClient(List<string> clientDesktopNames)
        {
            bool added = false;
            foreach (var n in clientDesktopNames)
            {
                if (!Config.Instance.Desktops.Any(d => string.Compare(d, n, StringComparison.InvariantCultureIgnoreCase) == 0))
                {
                    Config.Instance.Desktops.Add(n.NormalizeDesktopName());
                    added = true;
                }
            }

            if (added)
            {
                await Config.Save();
                DesktopsChanged?.Invoke(null, new EventArgs());
            }
        }

        public static async Task AddFromServer(List<string> serverDesktopNames)
        {
            if (!Config.Instance.Desktops.SequenceEqual(serverDesktopNames))
            {
                Config.Instance.Desktops = serverDesktopNames;
                await Config.Save();
                DesktopsChanged?.Invoke(null, new EventArgs());
            }
        }
    }
}
