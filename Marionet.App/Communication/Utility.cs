using System;
using System.Net;
using System.Net.Sockets;

namespace Marionet.App.Communication
{
    internal static class Utility
    {
        public const string NetHubPath = "/server";

        public static Uri GetNetHubUri(string host, int port) => new Uri($"https://{host}:{port}{NetHubPath}");

        public static Uri GetNetHubUri(IPAddress address, int port) => address.AddressFamily switch
        {
            AddressFamily.InterNetworkV6 => GetNetHubUri($"[{address}]", port),
            AddressFamily.InterNetwork => GetNetHubUri(address.ToString(), port),
            _ => throw new NotImplementedException($"The address type {address.AddressFamily} is not implemented.")
        };
    }
}
