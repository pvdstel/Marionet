using System;
using System.IO;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using System.Threading;

namespace Marionet.App.Configuration
{
    internal class CertificateManagement : IDisposable
    {
        private static readonly SemaphoreSlim creationSemaphore = new SemaphoreSlim(1, 1);
        private readonly X509Certificate2 serverCertificate;
        private readonly X509Certificate2 clientCertificate;
        private bool disposedValue;

        public CertificateManagement(ConfigurationService configurationService)
        {
            if (configurationService == null)
            {
                throw new ArgumentNullException(nameof(configurationService));
            }

            string serverCertificatePath = configurationService.Configuration.ServerCertificatePath;
            string clientCertificatePath = configurationService.Configuration.ClientCertificatePath;

            creationSemaphore.Wait();
            bool serverCertificateCreated = false;
            if (!File.Exists(serverCertificatePath))
            {
                X509Certificate2 certificate = BuildServerCertificate();
                File.WriteAllBytes(serverCertificatePath, certificate.Export(X509ContentType.Pfx));
                serverCertificateCreated = true;
            }
            serverCertificate = new X509Certificate2(serverCertificatePath);

            if (serverCertificateCreated || !File.Exists(clientCertificatePath))
            {
                X509Certificate2 certificate = BuildClientCertificate(serverCertificate);
                File.WriteAllBytes(clientCertificatePath, certificate.Export(X509ContentType.Pfx));
            }

            clientCertificate = new X509Certificate2(clientCertificatePath);
            creationSemaphore.Release();
        }

        public X509Certificate2 ServerCertificate => serverCertificate;

        public X509Certificate2 ClientCertificate => clientCertificate;

        public bool IsValidServerCertificate(X509Certificate2 serverCert) => IsParent(serverCert, clientCertificate);

        public bool IsValidClientCertificate(X509Certificate2 clientCert) => IsParent(ServerCertificate, clientCert);

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

            for (int i = 0; i < parentData.Length; ++i)
            {
                if (parentData[i] != childData[i])
                {
                    return false;
                }
            }

            return true;
        }

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

        protected virtual void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                if (disposing)
                {
                    serverCertificate.Dispose();
                    clientCertificate.Dispose();
                }

                disposedValue = true;
            }
        }

        public void Dispose()
        {
            Dispose(disposing: true);
            GC.SuppressFinalize(this);
        }
    }
}
