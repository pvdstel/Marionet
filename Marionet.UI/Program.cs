using Avalonia;
using Avalonia.Controls;
using Avalonia.ReactiveUI;
using Avalonia.Threading;
using Marionet.App;
using Marionet.UI.Views;
using System;
using System.Collections.Immutable;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Marionet.UI
{
    public static class Program
    {
        private const string ShowSignalName = "7e29830a-272c-4354-85e7-1a85a0e6a48c";
        private const string FirstMutexName = "d0ab88ed-49ac-45f1-b695-1ba3c6d23b1c";

        private static readonly CancellationTokenSource appShutdownToken = new CancellationTokenSource();
        private static readonly EventWaitHandle signal = new EventWaitHandle(false, EventResetMode.AutoReset, ShowSignalName);
        private static Mutex? isFirst;
        private static readonly Lazy<MainWindow> mainWindow = new Lazy<MainWindow>();

        public static void Main(string[] args)
        {
            InvariantArgs = args.Select(a => a.ToUpperInvariant()).ToImmutableList();

            isFirst = new Mutex(false, FirstMutexName, out bool createdNewMutex);
            if (!createdNewMutex || !isFirst.WaitOne(100))
            {
                signal.Set();
                return;
            }

            Supervisor.Initialize().Wait();

            try
            {
                var app = BuildAvaloniaApp()
                    .SetupWithoutStarting()
                    .Instance;

                if (InvariantArgs.Contains("START"))
                {
                    ShowMainWindow();
                    _ = Supervisor.StartAsync();
                }
                else if (InvariantArgs.Contains("START-SILENT"))
                {
                    _ = Supervisor.StartAsync();
                }
                else
                {
                    ShowMainWindow();
                }

                RunSignalWaitingThread();

                app.Run(appShutdownToken.Token);
            }
            finally
            {
                isFirst.ReleaseMutex();
            }
        }

        public static AppBuilder BuildAvaloniaApp() => AppBuilder.Configure<UIApp>()
            .UsePlatformDetect()
            .UseReactiveUI();

        private static void ShowMainWindow()
        {
            Dispatcher.UIThread.InvokeAsync(() =>
            {
                mainWindow.Value.Show();
                mainWindow.Value.Activate();
            });
        }

        private static void RunSignalWaitingThread()
        {
            Task.Factory.StartNew(() =>
            {
                while (!appShutdownToken.IsCancellationRequested)
                {
                    if (signal.WaitOne(5000))
                    {
                        ShowMainWindow();
                    }
                }
            }, appShutdownToken.Token, TaskCreationOptions.LongRunning, TaskScheduler.Default);
        }

        public static void ShutdownApplication()
        {
            appShutdownToken.Cancel();
        }

        public static ImmutableList<string> InvariantArgs { get; private set; } = ImmutableList<string>.Empty;
    }
}
