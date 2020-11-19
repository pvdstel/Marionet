using Marionet.App.Authentication;
using Marionet.App.Communication;
using Marionet.App.Configuration;
using Marionet.App.Core;
using Marionet.Core;
using Marionet.Core.Communication;
using Marionet.Core.Input;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using System;

namespace Marionet.App
{
    public class Startup
    {
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Reliability", "CA2000:Dispose objects before losing scope", Justification = "<Pending>")]
        public void ConfigureServices(IServiceCollection services)
        {
            ConfigurationService configurationService = new ConfigurationService();

            services.AddAuthentication(CertificateMatchAuthenticationDefaults.AuthenticationScheme).AddCertificateMatch(options =>
            {
                options.ServerCertificate = configurationService.CertificateManagement.ServerCertificate;
            });
            services.AddAuthorization(o =>
            {
                o.DefaultPolicy = new AuthorizationPolicyBuilder()
                    .AddAuthenticationSchemes(CertificateMatchAuthenticationDefaults.AuthenticationScheme)
                    .RequireAuthenticatedUser()
                    .Build();
            });
            services.AddSignalR();
                //.AddMessagePackProtocol();

            services.AddSingleton<ConfigurationService>(configurationService);
            services.AddSingleton<Supervisor>();
            services.AddSingleton<ClientIdentifierService>();
            services.AddSingleton<WorkspaceClientManager>();
            services.AddSingleton<ConfigurationSynchronizationService>();

            // Register services for Workspace
            services.AddSingleton<IConfigurationProvider, ConfigurationProvider>();
            services.AddSingleton<IInputManager>(services => PlatformSelector.GetInputManager());
            services.AddSingleton<WorkspaceNetwork>();
            services.AddSingleton<IWorkspaceNetwork>(services => services.GetService<WorkspaceNetwork>()!);

            // Register settings creation for Workspace
            services.AddTransient<WorkspaceSettings, WorkspaceSettingsService>();

            // Register Workspace
            services.AddSingleton<Workspace>();
        }

        public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {
            if (app == null)
            {
                throw new ArgumentNullException(nameof(app));
            }

            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }

            app.UseAuthentication();
            app.UseRouting();
            app.UseAuthorization();

            app.UseEndpoints(endpoints =>
            {
                endpoints.MapGet("/", async context =>
                {
                    await context.Response.WriteAsync("Marionet is running.");
                });
                endpoints.MapHub<NetHub>(Communication.Utility.NetHubPath, options =>
                {
                    options.Transports = Microsoft.AspNetCore.Http.Connections.HttpTransportType.WebSockets;
                });
            });

            app.ApplicationServices.GetService<Supervisor>(); // ensure that the application registers with Supervisor
            app.ApplicationServices.GetService<ConfigurationService>()!.Load().Wait();
            app.ApplicationServices.GetService<Workspace>()!.Initialize().Wait();
            app.ApplicationServices.GetService<WorkspaceClientManager>()!.Start();
        }
    }
}
