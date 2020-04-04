namespace Marionet.Core.Net
{
    public class WirelessNetworkInterface
    {
        public WirelessNetworkInterface(bool connected, string? ssid)
        {
            SSID = ssid;
            Connected = connected;
        }

        public bool Connected { get; }

        public string? SSID { get; }

        public override string ToString()
        {
            return $"WirelessNetworkInterface(Connected={Connected}, SSID={SSID})";
        }
    }
}
