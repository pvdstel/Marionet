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

#if DEBUG
            Console.WriteLine("This is a debug version of Marionet.");
#endif

            await Supervisor.Initialize();
            await Supervisor.StartAsync(args);
        }

    }
}
