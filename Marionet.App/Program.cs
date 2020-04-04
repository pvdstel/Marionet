using System.Threading.Tasks;

namespace Marionet.App
{
    public static class Program
    {
        public static Task Main(string[] args)
        {
            Configuration.Config.Load().Wait();
            return Supervisor.Run(args);
        }

    }
}
