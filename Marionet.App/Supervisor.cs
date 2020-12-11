﻿using Marionet.App.Configuration;
using Marionet.Core;
using Marionet.Core.Net;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Hosting;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Linq;
using System.Net.NetworkInformation;
using System.Threading;
using System.Threading.Tasks;

namespace Marionet.App
{
    public class Supervisor : IDisposable
    {
        private static Thread? appThread;
        private static TaskCompletionSource<TerminationReason>? stopReasonSource;
        private static TaskCompletionSource<bool>? roundEndSource;
        private static readonly INetwork network;
        private static readonly SemaphoreSlim checkRunningAllowedSemaphore ;
        private static readonly SemaphoreSlim runningSemaphore;
        private static readonly Dictionary<string, PeerConnectionStatuses> peerStatuses;

        private static readonly ConfigurationService configurationService;

        private readonly IHostApplicationLifetime applicationLifetime;

        public Supervisor(IHostApplicationLifetime applicationLifetime)
        {
            this.applicationLifetime = applicationLifetime ?? throw new ArgumentNullException(nameof(applicationLifetime));

            RunningAllowedUpdated += OnRunningAllowedUpdated;
            ShutdownRequested += OnShutdownRequested;
        }

        static Supervisor()
        {
            network = PlatformSelector.GetNetwork();
            checkRunningAllowedSemaphore = new SemaphoreSlim(1, 1);
            runningSemaphore = new SemaphoreSlim(1, 1);
            peerStatuses = new Dictionary<string, PeerConnectionStatuses>();
            configurationService = new ConfigurationService();

            configurationService.ConfigurationChanged += OnEnvironmentUpdate;
            NetworkChange.NetworkAddressChanged += OnEnvironmentUpdate;
        }

        public static bool SupervisorActive { get; private set; }
        public static bool RunningAllowed { get; private set; }
        public static bool HostRunning { get; private set; }

        public static IReadOnlyDictionary<string, PeerConnectionStatuses> PeerStatuses => new ReadOnlyDictionary<string, PeerConnectionStatuses>(peerStatuses);

        public static event EventHandler? Started;
        public static event EventHandler? Stopped;
        public static event EventHandler? RunningAllowedUpdated;
        public static event EventHandler? HostRunningUpdated;
        public static event EventHandler? PeerStatusesUpdated;

        private static event EventHandler? ShutdownRequested;

        public static async Task Initialize()
        {
            await configurationService.Load();
            await UpdateRunningAllowed();
        }

        public static Task StartAsync() => StartAsync(Array.Empty<string>());

        public static async Task StartAsync(string[] args)
        {
            await runningSemaphore.WaitAsync();
            if (SupervisorActive)
            {
                return;
            }
            SupervisorActive = true;
            runningSemaphore.Release();
            Started?.Invoke(null, new EventArgs());

            bool continueRunning = true;
            while (continueRunning)
            {
                await UpdateRunningAllowed();
                if (RunningAllowed)
                {
                    WriteDebugLine("Running is allowed by the run conditions.");
                    WriteDebugLine("Creating new host thread...");
                    RunApp(args);
                    WriteDebugLine("Host thread created.");

                    if (stopReasonSource == null || roundEndSource == null)
                    {
                        throw new InvalidOperationException("The stop reason task source is null. This state should be impossible.");
                    }

                    WriteDebugLine("Waiting for host termination...");
                    var stopReason = await stopReasonSource.Task;
                    var mayContinue = await roundEndSource.Task;
                    UpdateHostRunning(false);
                    WriteDebugLine($"Host termination reason was: {stopReason}");
                    if (!mayContinue || stopReason == TerminationReason.Self || stopReason == TerminationReason.ShutdownRequested)
                    {
                        continueRunning = false;
                    }
                }
                else
                {
                    WriteDebugLine("Running is not allowed by the run conditions.");
                    roundEndSource = new TaskCompletionSource<bool>();
                    EventHandler handler = default!;
                    handler = (sender, e) =>
                    {
                        if (RunningAllowed)
                        {
                            WriteDebugLine("Running is now allowed.");
                            RunningAllowedUpdated -= handler;
                            roundEndSource.TrySetResult(true);
                        }
                        else
                        {
                            WriteDebugLine("Running is still not allowed.");
                        }
                    };
                    RunningAllowedUpdated += handler;
                    continueRunning = await roundEndSource.Task;
                }
            }

            WriteDebugLine("Exiting...");

            await runningSemaphore.WaitAsync();
            SupervisorActive = false;
            runningSemaphore.Release();

            Stopped?.Invoke(null, new EventArgs());
        }

        public static async Task Stop()
        {
            await Task.Yield();
            if (appThread == null)
            {
                roundEndSource?.TrySetResult(false);
            }
            await Task.Run(() =>
            {
                ShutdownRequested?.Invoke(null, new EventArgs());
            });
        }

        public void SetPeerStatus(string peerName, PeerConnectionStatuses type, bool value)
        {
            string normalizedName = peerName.NormalizeDesktopName();
            PeerConnectionStatuses status = peerStatuses.ContainsKey(normalizedName) ? peerStatuses[normalizedName] : PeerConnectionStatuses.None;
            if (value)
            {
                status |= type;
            }
            else
            {
                status &= ~type;
            }
            if (status == PeerConnectionStatuses.None)
            {
                peerStatuses.Remove(peerName);
            }
            else
            {
                peerStatuses[normalizedName] = status;
            }
            PeerStatusesUpdated?.Invoke(this, new EventArgs());
        }

        private static void RunApp(string[] args)
        {
            stopReasonSource = new TaskCompletionSource<TerminationReason>(TaskCreationOptions.RunContinuationsAsynchronously);
            roundEndSource = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
            appThread = new Thread(() =>
            {
                UpdateHostRunning(true);
                try
                {
                    CreateHostBuilder(args).Build().Run();
                }
                catch (OperationCanceledException) { }
                stopReasonSource?.TrySetResult(TerminationReason.Self);
                appThread = null;
                WriteDebugLine("Host application thread finished.");
                roundEndSource.TrySetResult(true);
            });
            appThread.Start();
        }

        private static void OnEnvironmentUpdate(object? sender, EventArgs e )
        {
            _ = UpdateRunningAllowed();
        }

        private static async Task UpdateRunningAllowed()
        {
            await checkRunningAllowedSemaphore.WaitAsync();
            var runningAllowed = await IsRunningAllowed();
            RunningAllowed = runningAllowed;
            RunningAllowedUpdated?.Invoke(null, new EventArgs());
            checkRunningAllowedSemaphore.Release();
        }

        private static async Task<bool> IsRunningAllowed()
        {
            if (configurationService.Configuration.RunConditions.BlockAll)
            {
                return false;
            }

            var wirelessNetworks = await network.GetWirelessNetworkInterfaces();
            var connectedSsids = wirelessNetworks.Where(n => n.Connected).Select(n => n.SSID).ToList();
            var allowedSsids = configurationService.Configuration.RunConditions.AllowedSsids;
            var allowedBySsids = !allowedSsids.Any() || allowedSsids.Any(ssid => connectedSsids.Contains(ssid));

            var networkInterfaces = NetworkInterface.GetAllNetworkInterfaces();
            var allowedNetworkInterfaces = configurationService.Configuration.RunConditions.AllowedNetworkInterfaces;
            var allowedByInterface = !allowedNetworkInterfaces.Any() || networkInterfaces.Any(intf => intf.OperationalStatus == OperationalStatus.Up && allowedNetworkInterfaces.Contains(intf.Name));

            return allowedBySsids || allowedByInterface;
        }

        private static void UpdateHostRunning(bool running)
        {
            HostRunning = running;
            HostRunningUpdated?.Invoke(null, new EventArgs());
        }

        private void OnRunningAllowedUpdated(object? sender, EventArgs e)
        {
            if (!RunningAllowed)
            {
                WriteDebugLine("Running no longer allowed -- terminating host application.");
                stopReasonSource?.TrySetResult(TerminationReason.Disallowed);
                applicationLifetime.StopApplication();
            }
        }

        private void OnShutdownRequested(object? sender, EventArgs e)
        {
            WriteDebugLine("Shutdown request received -- terminating host application.");
            stopReasonSource?.TrySetResult(TerminationReason.ShutdownRequested);
            applicationLifetime.StopApplication();
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
                             o.ServerCertificate = configurationService.CertificateManagement.ServerCertificate;
                         });
                     });
                     webBuilder.UseUrls($"https://0.0.0.0:{Config.ServerPort}", $"https://[::]:{Config.ServerPort}");
                 });

        [System.Diagnostics.Conditional("DEBUG")]
        private static void WriteDebugLine(string message)
        {
            Debug.WriteLine(message);
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
            /// <summary>
            /// An external shutdown request was sent to the <see cref="Supervisor"/>.
            /// </summary>
            ShutdownRequested,
        }

        [Flags]
        public enum PeerConnectionStatuses
        {
            None = 0,
            IsClient = 1,
            IsServer = 2,
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
                    ShutdownRequested -= OnShutdownRequested;

                    peerStatuses.Clear();
                    PeerStatusesUpdated?.Invoke(this, new EventArgs());
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
