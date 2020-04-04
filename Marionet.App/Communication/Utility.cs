using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Marionet.App.Communication
{
    internal static class Utility
    {
        public const string NetHubPath = "/server";

        public static Uri GetNetHubUri(string host, int port) => new Uri($"https://{host}:{port}{NetHubPath}");
    }
}
