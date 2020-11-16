using Marionet.App.Configuration;
using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System;
using System.Linq;
using System.Security.Claims;
using System.Text.Encodings.Web;
using System.Threading.Tasks;

namespace Marionet.App.Authentication
{
    public class CertificateMatchAuthenticationHandler : AuthenticationHandler<CertificateMatchAuthenticationOptions>
    {
        public CertificateMatchAuthenticationHandler(
            IOptionsMonitor<CertificateMatchAuthenticationOptions> options,
            ILoggerFactory logger,
            UrlEncoder encoder,
            ISystemClock clock) : base(options, logger, encoder, clock)
        {
        }

        protected override async Task<AuthenticateResult> HandleAuthenticateAsync()
        {
            if (Options.ServerCertificate == null)
            {
                throw new InvalidOperationException("The server certificate is not set.");
            }

            // You only get client certificates over HTTPS
            if (!Context.Request.IsHttps)
            {
                return AuthenticateResult.NoResult();
            }

            var clientCertificate = await Context.Connection.GetClientCertificateAsync();
            if (clientCertificate == null)
            {
                return AuthenticateResult.NoResult();
            }

            if (clientCertificate.SubjectName.RawData.SequenceEqual(clientCertificate.IssuerName.RawData))
            {
                // The certificate is self-signed.
                return AuthenticateResult.Fail("The certificate may not be self-signed.");
            }

            if (CertificateManagement.IsParent(Options.ServerCertificate, clientCertificate))
            {
                ClaimsPrincipal claimsPrincipal = new ClaimsPrincipal(new ClaimsIdentity(CertificateMatchAuthenticationDefaults.AuthenticationScheme));
                return AuthenticateResult.Success(new AuthenticationTicket(claimsPrincipal, CertificateMatchAuthenticationDefaults.AuthenticationScheme));
            } else
            {
                return AuthenticateResult.Fail("The certificate is not signed by the server certificate.");
            }
        }

        protected override Task HandleChallengeAsync(AuthenticationProperties properties)
        {
            // Certificate authentication takes place at the connection level. We can't prompt once we're in
            // user code, so the best thing to do is Forbid, not Challenge.
            Response.StatusCode = 403;
            return Task.CompletedTask;
        }

        protected override Task HandleForbiddenAsync(AuthenticationProperties properties)
        {
            Response.StatusCode = 403;
            return Task.CompletedTask;
        }
    }
}
