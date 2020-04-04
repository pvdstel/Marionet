using Marionet.App.Configuration;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Hosting;

namespace Marionet.App
{
    public static class Program
    {
        public static void Main(string[] args)
        {
            Configuration.Config.Load().Wait();
            CreateHostBuilder(args).Build().Run();
        }

        public static IHostBuilder CreateHostBuilder(string[] args) =>
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
                    webBuilder.UseUrls($"https://0.0.0.0:{Configuration.Config.ServerPort}");
                });
    }
}
