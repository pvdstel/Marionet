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
        private static readonly CancellationTokenSource appShutdownToken = new CancellationTokenSource();
        private static readonly Lazy<MainWindow> mainWindow = new Lazy<MainWindow>();

        public static void Main(string[] args)
        {
            InvariantArgs = args.Select(a => a.ToUpperInvariant()).ToImmutableList();

            App.Program.RunSingleton(() =>
            {
                Supervisor.Initialize().Wait();

                var app = BuildAvaloniaApp()
                    .SetupWithoutStarting()
                    .Instance;

                if (InvariantArgs.Contains("START-SILENT"))
                {
                    _ = Supervisor.StartAsync();
                }
                else
                {
                    ShowMainWindow();
                }

                RunSignalWaitingThread();

                app.Run(appShutdownToken.Token);
            });
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
                    if (App.Program.WaitSingletonSignal(5000))
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
