using Marionet.Core.Net;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace Marionet.Core.Windows
{
    public class WindowsNetwork : INetwork
    {
        private Regex netshWlanRegex = new Regex("^\\s*(?<name>(?:\\w|\\s)+?)\\s+:\\s+(?<value>.+?)\\s*$");

        public Task<List<WirelessNetworkInterface>> GetWirelessNetworkInterfaces()
        {
            return Task.Run(async () =>
            {
                var interfaces = new List<WirelessNetworkInterface>();
                using (Process p = new Process())
                {
                    p.StartInfo.FileName = "netsh.exe";
                    p.StartInfo.Arguments = "wlan show interfaces";
                    p.StartInfo.RedirectStandardOutput = true;
                    p.StartInfo.UseShellExecute = false;
                    p.Start();
                    p.WaitForExit();

                    bool? connected = null;
                    string? ssid = null;
                    while (!p.StandardOutput.EndOfStream)
                    {
                        var nextLine = await p.StandardOutput.ReadLineAsync();
                        if (string.IsNullOrEmpty(nextLine) || string.IsNullOrWhiteSpace(nextLine))
                        {
                            if (connected.HasValue)
                            {
                                interfaces.Add(new WirelessNetworkInterface(connected.Value, ssid));
                            }
                            connected = null;
                            ssid = null;
                        }
                        else
                        {
                            var match = netshWlanRegex.Match(nextLine);
                            if (match.Success)
                            {
                                switch (match.Groups["name"].Value.ToUpperInvariant())
                                {
                                    case "STATE":
                                        connected = match.Groups["value"].Value.ToUpperInvariant() == "CONNECTED";
                                        break;
                                    case "SSID":
                                        ssid = match.Groups["value"].Value;
                                        break;
                                }
                            }
                        }
                    }
                }
                return interfaces;
            });
        }
    }
}
