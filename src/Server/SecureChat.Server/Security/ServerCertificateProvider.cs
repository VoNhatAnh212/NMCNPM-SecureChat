#nullable enable

using System.Net;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;

namespace SecureChat.Server.Security;

/// <summary>
/// Loads or creates the HTTPS certificate used by the development server.
/// </summary>
public static class ServerCertificateProvider
{
    /// <summary>Loads an existing certificate or creates one when the configured file is missing.</summary>
    public static X509Certificate2 LoadOrCreate(IConfiguration configuration, IWebHostEnvironment environment, ILogger logger)
    {
        var certPath = ResolvePath(
            environment.ContentRootPath,
            configuration["ServerCertificate:Path"] ?? "certs/securechat-dev-cert.pfx");

        var caCertPath = ResolvePath(
            environment.ContentRootPath,
            configuration["ServerCertificate:CaCertPath"] ?? "certs/ca.cert");

        var caKeyPath = ResolvePath(
            environment.ContentRootPath,
            configuration["ServerCertificate:CaKeyPath"] ?? "certs/securechat-ca.pfx");

        var password = configuration["ServerCertificate:Password"] ?? "securechat-dev-cert";
        Directory.CreateDirectory(Path.GetDirectoryName(certPath)!);
        Directory.CreateDirectory(Path.GetDirectoryName(caCertPath)!);
        Directory.CreateDirectory(Path.GetDirectoryName(caKeyPath)!);

        using var caCertificate = LoadOrCreateCaCertificate(caCertPath, caKeyPath, password);
        var certificate = LoadExistingServerCertificate(certPath, password);
        if (certificate is null || !IsSignedByCa(certificate, caCertificate))
        {
            certificate?.Dispose();
            certificate = CreateServerCertificate(certPath, password, caCertificate);
        }

        var sha256 = Convert.ToHexString(SHA256.HashData(certificate.RawData));
        logger.LogInformation(
            "HTTPS certificate loaded. Issuer: {Issuer}. SHA-256: {CertificateSha256}. CA: {CaPath}",
            certificate.Issuer,
            sha256,
            caCertPath);

        return certificate;
    }

    private static X509Certificate2? LoadExistingServerCertificate(string certPath, string password)
    {
        if (!File.Exists(certPath))
        {
            return null;
        }

        return new X509Certificate2(certPath, password, X509KeyStorageFlags.Exportable | X509KeyStorageFlags.MachineKeySet);
    }

    private static X509Certificate2 LoadOrCreateCaCertificate(string caCertPath, string caKeyPath, string password)
    {
        if (File.Exists(caKeyPath))
        {
            var existingCa = new X509Certificate2(caKeyPath, password, X509KeyStorageFlags.Exportable | X509KeyStorageFlags.MachineKeySet);
            File.WriteAllBytes(caCertPath, existingCa.Export(X509ContentType.Cert));
            return existingCa;
        }

        using var rsa = RSA.Create(4096);
        var request = new CertificateRequest(
            "CN=SecureChat Local Root CA",
            rsa,
            HashAlgorithmName.SHA256,
            RSASignaturePadding.Pkcs1);

        request.CertificateExtensions.Add(new X509BasicConstraintsExtension(true, false, 0, true));
        request.CertificateExtensions.Add(new X509KeyUsageExtension(
            X509KeyUsageFlags.KeyCertSign | X509KeyUsageFlags.CrlSign,
            true));
        request.CertificateExtensions.Add(new X509SubjectKeyIdentifierExtension(request.PublicKey, false));

        var now = DateTimeOffset.UtcNow;
        using var ca = request.CreateSelfSigned(now.AddDays(-1), now.AddYears(10));
        var exportableCa = new X509Certificate2(
            ca.Export(X509ContentType.Pfx, password),
            password,
            X509KeyStorageFlags.Exportable | X509KeyStorageFlags.MachineKeySet);

        File.WriteAllBytes(caKeyPath, exportableCa.Export(X509ContentType.Pfx, password));
        File.WriteAllBytes(caCertPath, exportableCa.Export(X509ContentType.Cert));
        return exportableCa;
    }

    private static X509Certificate2 CreateServerCertificate(string certPath, string password, X509Certificate2 caCertificate)
    {
        using var rsa = RSA.Create(4096);
        var request = new CertificateRequest(
            "CN=SecureChat Development Server",
            rsa,
            HashAlgorithmName.SHA256,
            RSASignaturePadding.Pkcs1);

        request.CertificateExtensions.Add(new X509BasicConstraintsExtension(false, false, 0, false));
        request.CertificateExtensions.Add(new X509KeyUsageExtension(
            X509KeyUsageFlags.DigitalSignature | X509KeyUsageFlags.KeyEncipherment,
            false));
        request.CertificateExtensions.Add(new X509EnhancedKeyUsageExtension(
            new OidCollection { new("1.3.6.1.5.5.7.3.1") },
            false));

        var sanBuilder = new SubjectAlternativeNameBuilder();
        sanBuilder.AddDnsName("localhost");
        sanBuilder.AddIpAddress(IPAddress.Loopback);
        sanBuilder.AddIpAddress(IPAddress.IPv6Loopback);
        request.CertificateExtensions.Add(sanBuilder.Build());
        request.CertificateExtensions.Add(new X509SubjectKeyIdentifierExtension(request.PublicKey, false));

        var now = DateTimeOffset.UtcNow;
        var serialNumber = RandomNumberGenerator.GetBytes(16);
        using var certificate = request.Create(
            caCertificate,
            now.AddDays(-1),
            now.AddYears(3),
            serialNumber);

        var exportableCertificate = new X509Certificate2(
            certificate.CopyWithPrivateKey(rsa).Export(X509ContentType.Pfx, password),
            password,
            X509KeyStorageFlags.Exportable | X509KeyStorageFlags.MachineKeySet);

        File.WriteAllBytes(certPath, exportableCertificate.Export(X509ContentType.Pfx, password));
        return exportableCertificate;
    }

    private static bool IsSignedByCa(X509Certificate2 certificate, X509Certificate2 caCertificate)
    {
        if (!string.Equals(certificate.Issuer, caCertificate.Subject, StringComparison.Ordinal))
        {
            return false;
        }

        using var chain = new X509Chain();
        chain.ChainPolicy.TrustMode = X509ChainTrustMode.CustomRootTrust;
        chain.ChainPolicy.CustomTrustStore.Add(caCertificate);
        chain.ChainPolicy.RevocationMode = X509RevocationMode.NoCheck;
        chain.ChainPolicy.VerificationFlags = X509VerificationFlags.AllowUnknownCertificateAuthority;
        return chain.Build(certificate);
    }

    private static string ResolvePath(string contentRoot, string path)
    {
        return Path.IsPathRooted(path)
            ? path
            : Path.GetFullPath(Path.Combine(contentRoot, path));
    }
}
