using Marionet.App.Configuration;
using Marionet.Core.Net;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Hosting;
using System;
using System.Linq;
using System.Net.NetworkInformation;
using System.Threading;
using System.Threading.Tasks;

namespace Marionet.App
{
    public class Supervisor : IDisposable
    {
        private static bool runCalled;
        private static Thread? appThread;
        private static TaskCompletionSource<TerminationReason>? stopReasonSource;
        private static readonly INetwork network = PlatformSelector.GetNetwork();
        private static readonly SemaphoreSlim checkSemaphore = new SemaphoreSlim(1, 1);

        private readonly IHostApplicationLifetime applicationLifetime;

        public Supervisor(IHostApplicationLifetime applicationLifetime)
        {
            this.applicationLifetime = applicationLifetime ?? throw new ArgumentNullException(nameof(applicationLifetime));
        }

        private static event EventHandler<RunningAllowedChangedEventArgs>? RunningAllowedUpdated;

        public static async Task Run(string[] args)
        {
            if (runCalled)
            {
                throw new InvalidOperationException("The " + nameof(Run) + " method has already been called.");
            }
            runCalled = true;

            StartEnvironmentMonitor();

            bool continueRunning = true;
            while (continueRunning)
            {
                if (await IsRunningAllowed())
                {
                    WriteDebugLine("Running is allowed by the run conditions.");
                    WriteDebugLine("Creating new host thread...");
                    RunApp(args);
                    WriteDebugLine("Host thread created.");

                    if (stopReasonSource == null)
                    {
                        throw new InvalidOperationException("The stop reason task source is null. This state should be impossible.");
                    }

                    WriteDebugLine("Waiting for host termination...");
                    var stopReason = await stopReasonSource.Task;
                    WriteDebugLine($"Host termination reason was: {stopReason}");
                    if (stopReason == TerminationReason.Self)
                    {
                        continueRunning = false;
                    }
                }
                else
                {
                    WriteDebugLine("Running is not allowed by the run conditions.");
                    TaskCompletionSource<bool> wait = new TaskCompletionSource<bool>();
                    EventHandler<RunningAllowedChangedEventArgs> handler = default!;
                    handler = (sender, e) =>
                    {
                        if (e.RunningAllowed)
                        {
                            WriteDebugLine("Running is now allowed.");
                            RunningAllowedUpdated -= handler;
                            wait.TrySetResult(true);
                        }
                        else
                        {
                            WriteDebugLine("Running is still not allowed.");
                        }
                    };
                    RunningAllowedUpdated += handler;
                    await wait.Task;
                }
            }
            WriteDebugLine("Exiting...");
        }

        public void StartMonitoring()
        {
            RunningAllowedUpdated += OnRunningAllowedUpdated;
        }

        [System.Diagnostics.Conditional("DEBUG")]
        private static void WriteDebugLine(string message)
        {
            Console.WriteLine(message);
        }

        private static void RunApp(string[] args)
        {
            stopReasonSource = new TaskCompletionSource<TerminationReason>(TaskCreationOptions.RunContinuationsAsynchronously);
            appThread = new Thread(() =>
            {
                CreateHostBuilder(args).Build().Run();
                stopReasonSource?.TrySetResult(TerminationReason.Self);
                appThread = null;
                WriteDebugLine("Host application thread finished.");
            });
            appThread.Start();
        }

        private static void StartEnvironmentMonitor()
        {
            NetworkChange.NetworkAddressChanged += (sender, e) =>
            {
                _ = UpdateRunningAllowed();
            };
            Config.SettingsReloaded += (sender, e) =>
            {
                _ = UpdateRunningAllowed();
            };
        }

        private static async Task UpdateRunningAllowed()
        {
            await checkSemaphore.WaitAsync();
            var runningAllowed = await IsRunningAllowed();
            RunningAllowedUpdated?.Invoke(null, new RunningAllowedChangedEventArgs(runningAllowed));
            checkSemaphore.Release();
        }

        private static async Task<bool> IsRunningAllowed()
        {
            var wirelessNetworks = await network.GetWirelessNetworkInterfaces();
            var connectedSsids = wirelessNetworks.Where(n => n.Connected).Select(n => n.SSID).ToList();
            var allowedSsids = Config.Instance.RunConditions.AllowedSsids;
            if (allowedSsids.Any() && !allowedSsids.Any(ssid => connectedSsids.Contains(ssid)))
            {
                return false;
            }

            return true;
        }

        private static IHostBuilder CreateHostBuilder(string[] args) =>
             Host.CreateDefaultBuilder(args)
                 .ConfigureWebHostDefaults(webBuilder =>
                 {
                     webBuilder.UseStartup<Startup>();
                     webBuilder.ConfigureKestrel(options =>
                     {
                         options.ConfigureHttpsDefaults(o =>
                         {
                             o.ClientCertificateMode = Microsoft.AspNetCore.Server.Kestrel.Https.ClientCertificateMode.RequireCertificate;
                             o.ClientCertificateValidation = (certificate, chain, sslPolicyErrors) => true;
                             o.ServerCertificate = Certificate.ServerCertificate;
                         });
                     });
                     webBuilder.UseUrls($"https://0.0.0.0:{Config.ServerPort}");
                 });

        private void OnRunningAllowedUpdated(object? sender, RunningAllowedChangedEventArgs e)
        {
            if (!e.RunningAllowed)
            {
                WriteDebugLine("Running no longer allowed -- terminating host application.");
                stopReasonSource?.TrySetResult(TerminationReason.Disallowed);
                applicationLifetime.StopApplication();
            }
        }

        /// <summary>
        /// Defines the reason an application has stopped.
        /// </summary>
        private enum TerminationReason
        {
            /// <summary>
            /// The application terminated by itself.
            /// </summary>
            Self,
            /// <summary>
            /// Execution of the application was no longer allowed and therefore requested to stop.
            /// </summary>
            Disallowed,
        }

        private class RunningAllowedChangedEventArgs : EventArgs
        {
            public RunningAllowedChangedEventArgs(bool runningAllowed)
            {
                RunningAllowed = runningAllowed;
            }

            public bool RunningAllowed { get; }
        }

        #region IDisposable Support
        private bool disposedValue = false;

        protected virtual void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                if (disposing)
                {
                    RunningAllowedUpdated -= OnRunningAllowedUpdated;
                }

                disposedValue = true;
            }
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }
        #endregion
    }
}
