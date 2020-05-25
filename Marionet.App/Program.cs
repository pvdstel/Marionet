using System;
using System.Reflection;
using System.Threading.Tasks;

namespace Marionet.App
{
    public static class Program
    {
        public static async Task Main(string[] args)
        {
            var appVersion = Assembly.GetExecutingAssembly().GetName().Version?.ToString(3) ?? "?";
            Console.WriteLine($"Marionet {appVersion}");
            await Supervisor.Initialize();
            await Supervisor.StartAsync(args);
        }

    }
}
