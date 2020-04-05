using System.Threading.Tasks;

namespace Marionet.App
{
    public static class Program
    {
        public static async Task Main(string[] args)
        {
            await Supervisor.Initialize();
            await Supervisor.StartAsync(args);
        }

    }
}
