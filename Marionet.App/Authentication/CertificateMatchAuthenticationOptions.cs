using Microsoft.AspNetCore.Authentication;
using System.Security.Cryptography.X509Certificates;

namespace Marionet.App.Authentication
{
    public class CertificateMatchAuthenticationOptions : AuthenticationSchemeOptions
    {
        public X509Certificate2? ServerCertificate { get; set; }
    }
}
