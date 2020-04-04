using Marionet.App.Authentication;
using Marionet.App.Communication;
using Marionet.App.Configuration;
using Marionet.App.Core;
using Marionet.Core;
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
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Performance", "CA1822:Mark members as static", Justification = "Called by the ASP.NET Core runtime.")]
        public void ConfigureServices(IServiceCollection services)
        {
            services.AddAuthentication(CertificateMatchAuthenticationDefaults.AuthenticationScheme).AddCertificateMatch(options =>
            {
                options.ServerCertificate = Certificate.ServerCertificate;
            });
            services.AddAuthorization(o =>
            {
                o.DefaultPolicy = new AuthorizationPolicyBuilder()
                    .AddAuthenticationSchemes(CertificateMatchAuthenticationDefaults.AuthenticationScheme)
                    .RequireAuthenticatedUser()
                    .Build();
            });
            services.AddSignalR()
                .AddMessagePackProtocol();

            services.AddSingleton<ClientIdentifierService>();
            services.AddSingleton<WorkspaceClientManager>();
            services.AddSingleton<WorkspaceNetwork>();
            services.AddSingleton<IInputManager>(services => InputManagerSelector.GetInputManager());
            services.AddTransient<WorkspaceSettings, WorkspaceSettingsService>();
            services.AddSingleton<Workspace>();
        }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Performance", "CA1822:Mark members as static", Justification = "Called by the ASP.NET Core runtime.")]
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

            app.ApplicationServices.GetService<Workspace>().Initialize().Wait();
            app.ApplicationServices.GetService<WorkspaceClientManager>().Start();
        }
    }
}
