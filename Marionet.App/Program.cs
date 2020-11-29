using System;
using System.Reflection;
using System.Threading;

namespace Marionet.App
{
    public static class Program
    {
        private const string ShowSignalName = "Marionet:7e29830a-272c-4354-85e7-1a85a0e6a48c";
        private const string FirstMutexName = "Marionet:d0ab88ed-49ac-45f1-b695-1ba3c6d23b1c";

        private static readonly EventWaitHandle signal = new EventWaitHandle(false, EventResetMode.AutoReset, ShowSignalName);
        private static Mutex? isFirst;

        public static void Main(string[] args)
        {
            var appVersion = Assembly.GetExecutingAssembly().GetName().Version?.ToString(3) ?? "?";
            Console.WriteLine($"Marionet {appVersion}");

#if DEBUG
            Console.WriteLine("This is a debug version of Marionet.");
#endif

            RunSingleton(() =>
            {
                Supervisor.Initialize().Wait();
                Supervisor.StartAsync(args).Wait();
            });
        }
        
        public static void RunSingleton(Action singleton)
        {
            if (singleton == null)
            {
                throw new ArgumentNullException(nameof(singleton));
            }

            isFirst = new Mutex(false, FirstMutexName, out bool createdNewMutex);
            if (!createdNewMutex || !isFirst.WaitOne(100))
            {
                signal.Set();
                Console.WriteLine("Marionet is already running. This instance will exit.");
                Environment.Exit(0);
            }

            try
            {
                singleton.Invoke();
            }
            finally
            {
                isFirst.ReleaseMutex();
            }
        }

        public static bool WaitSingletonSignal(int millisecondsTimeout = 5000)
        {
            return signal.WaitOne(millisecondsTimeout);
        }
    }
}
