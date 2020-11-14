using System;
using System.IO;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;

namespace Marionet.App.Configuration
{
    internal static class Certificate
    {
        private static X509Certificate2 BuildServerCertificate()
        {
            string certificateName = $"Marionet Server {Guid.NewGuid()}";
            SubjectAlternativeNameBuilder sanBuilder = new SubjectAlternativeNameBuilder();
            sanBuilder.AddUserPrincipalName("Marionet Self-Signed Certificate Generator");
            X500DistinguishedName distinguishedName = new X500DistinguishedName($"CN={certificateName}");

            using RSA rsa = RSA.Create(2048);
            var request = new CertificateRequest(distinguishedName, rsa, HashAlgorithmName.SHA256, RSASignaturePadding.Pkcs1);

            request.CertificateExtensions.Add(new X509BasicConstraintsExtension(true, false, 0, true));
            request.CertificateExtensions.Add(new X509SubjectKeyIdentifierExtension(request.PublicKey, false));
            request.CertificateExtensions.Add(
                new X509KeyUsageExtension(X509KeyUsageFlags.KeyCertSign | X509KeyUsageFlags.KeyEncipherment | X509KeyUsageFlags.KeyAgreement, false));

            request.CertificateExtensions.Add(
               new X509EnhancedKeyUsageExtension(
                   new OidCollection { new Oid("1.3.6.1.5.5.7.3.1"), new Oid("1.3.6.1.5.5.7.3.2") }, false));

            request.CertificateExtensions.Add(sanBuilder.Build());

            var certificate = request.CreateSelfSigned(new DateTimeOffset(DateTime.UtcNow.AddDays(-30)), new DateTimeOffset(DateTime.UtcNow.AddDays(3650 + 1)));
            certificate.FriendlyName = certificateName;

            return certificate;
        }

        private static X509Certificate2 BuildClientCertificate(X509Certificate2 serverCertificate)
        {
            string certificateName = $"Marionet Client {Guid.NewGuid()}";
            SubjectAlternativeNameBuilder sanBuilder = new SubjectAlternativeNameBuilder();
            sanBuilder.AddUserPrincipalName("Marionet Self-Signed Certificate Generator");
            X500DistinguishedName distinguishedName = new X500DistinguishedName($"CN={certificateName}");

            using RSA rsa = RSA.Create(2048);
            var request = new CertificateRequest(distinguishedName, rsa, HashAlgorithmName.SHA256, RSASignaturePadding.Pkcs1);

            request.CertificateExtensions.Add(new X509BasicConstraintsExtension(false, false, 0, false));
            request.CertificateExtensions.Add(new X509SubjectKeyIdentifierExtension(request.PublicKey, false));
            request.CertificateExtensions.Add(
                new X509KeyUsageExtension(X509KeyUsageFlags.KeyEncipherment | X509KeyUsageFlags.DigitalSignature | X509KeyUsageFlags.KeyAgreement, false));

            request.CertificateExtensions.Add(
               new X509EnhancedKeyUsageExtension(
                   new OidCollection { new Oid("1.3.6.1.5.5.7.3.2") }, false));

            request.CertificateExtensions.Add(sanBuilder.Build());

            var certificate = request.Create(serverCertificate, new DateTimeOffset(DateTime.UtcNow.AddDays(-1)), new DateTimeOffset(DateTime.UtcNow.AddDays(3650)), new byte[] { 2, 4, 8, 16 });
            certificate.FriendlyName = certificateName;

            return certificate.CopyWithPrivateKey(rsa);
        }

        private static bool serverCertificateCreated = false;
        private static readonly Lazy<X509Certificate2> serverCertificate = new Lazy<X509Certificate2>(() =>
        {
            if (!File.Exists(Config.Instance.ServerCertificatePath))
            {
                X509Certificate2 certificate = BuildServerCertificate();
                File.WriteAllBytes(Config.Instance.ServerCertificatePath, certificate.Export(X509ContentType.Pfx));
                serverCertificateCreated = true;
            }

            return new X509Certificate2(Config.Instance.ServerCertificatePath);
        });
        public static X509Certificate2 ServerCertificate => serverCertificate.Value;

        private static readonly Lazy<X509Certificate2> clientCertificate = new Lazy<X509Certificate2>(() =>
        {
            var serverCert = ServerCertificate;
            if (serverCertificateCreated || !File.Exists(Config.Instance.ClientCertificatePath))
            {
                X509Certificate2 certificate = BuildClientCertificate(serverCert);
                File.WriteAllBytes(Config.Instance.ClientCertificatePath, certificate.Export(X509ContentType.Pfx));
            }

            return new X509Certificate2(Config.Instance.ClientCertificatePath);
        });
        public static X509Certificate2 ClientCertificate => clientCertificate.Value;

        public static bool IsParent(X509Certificate2 parent, X509Certificate2 child)
        {
            if (parent == null)
            {
                throw new ArgumentNullException(nameof(parent));
            }
            if (child == null)
            {
                throw new ArgumentNullException(nameof(child));
            }

            byte[] parentData = parent.SubjectName.RawData;
            byte[] childData = child.IssuerName.RawData;
            if (parentData.Length != childData.Length)
            {
                return false;
            }

            for (int i = 0; i < parentData.Length; ++i) {
                if (parentData[i] != childData[i])
                {
                    return false;
                }
            }

            return true;
        }
    }
}
