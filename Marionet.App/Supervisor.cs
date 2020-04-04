using Marionet.App.Configuration;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Hosting;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace Marionet.App
{
    public class Supervisor
    {
        private static Thread? appThread;
        private static TaskCompletionSource<StopReason>? stopReasonSource;

        private readonly IHostApplicationLifetime applicationLifetime;

        public Supervisor(IHostApplicationLifetime applicationLifetime)
        {
            this.applicationLifetime = applicationLifetime ?? throw new ArgumentNullException(nameof(applicationLifetime));
        }

        public static async Task Run(string[] args)
        {
            while (true) {
                Console.WriteLine("Creating new application thread...");
                RunApp(args);
                Console.WriteLine("Application thread created.");

                if (stopReasonSource == null)
                {
                    throw new InvalidOperationException("The stop reason task source is null. This state should be impossible.");
                }

                Console.WriteLine("Waiting for application exit...");
                var stopReason = await stopReasonSource.Task;
                Console.WriteLine($"Application exit reason was: {stopReason}");
                if (stopReason == StopReason.SelfTerminated)
                {
                    break;
                }
                Console.WriteLine("Restarting application...");
            }
            Console.WriteLine("Exiting...");
        }
        
        private static void RunApp(string[] args)
        {
            stopReasonSource = new TaskCompletionSource<StopReason>(TaskCreationOptions.RunContinuationsAsynchronously);
            appThread = new Thread(() =>
            {
                CreateHostBuilder(args).Build().Run();
                stopReasonSource.TrySetResult(StopReason.SelfTerminated);
                appThread = null;
            });
            appThread.Start();
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

        /// <summary>
        /// Defines the reason an application has stopped.
        /// </summary>
        private enum StopReason
        {
            /// <summary>
            /// The application terminated by itself.
            /// </summary>
            SelfTerminated,
            /// <summary>
            /// Execution of the application was no longer allowed and therefore requested to stop.
            /// </summary>
            Disallowed,
        }
    }
}
