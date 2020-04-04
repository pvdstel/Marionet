using Microsoft.AspNetCore.Authentication;
using System;

namespace Marionet.App.Authentication
{
    public static class CertificateMatchAuthenticationAppBuilderExtensions
    {
        public static AuthenticationBuilder AddCertificateMatch(this AuthenticationBuilder builder)
           => builder.AddCertificateMatch(CertificateMatchAuthenticationDefaults.AuthenticationScheme);

        public static AuthenticationBuilder AddCertificateMatch(this AuthenticationBuilder builder, string authenticationScheme)
            => builder.AddCertificateMatch(authenticationScheme, o => { });

        public static AuthenticationBuilder AddCertificateMatch(this AuthenticationBuilder builder, Action<CertificateMatchAuthenticationOptions> configureOptions)
            => builder.AddCertificateMatch(CertificateMatchAuthenticationDefaults.AuthenticationScheme, configureOptions);

        public static AuthenticationBuilder AddCertificateMatch(
            this AuthenticationBuilder builder,
            string authenticationScheme,
            Action<CertificateMatchAuthenticationOptions> configureOptions)
        {
            if (builder == null)
            {
                throw new ArgumentNullException(nameof(builder));
            }

            return builder.AddScheme<CertificateMatchAuthenticationOptions, CertificateMatchAuthenticationHandler>(authenticationScheme, configureOptions);
        }
    }
}
