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

        var hashPath = ResolvePath(
            environment.ContentRootPath,
            configuration["ServerCertificate:Sha256Path"] ?? "certs/securechat-dev-cert.sha256");

        var password = configuration["ServerCertificate:Password"] ?? "securechat-dev-cert";
        Directory.CreateDirectory(Path.GetDirectoryName(certPath)!);
        Directory.CreateDirectory(Path.GetDirectoryName(hashPath)!);

        var certificate = File.Exists(certPath)
            ? new X509Certificate2(certPath, password, X509KeyStorageFlags.Exportable | X509KeyStorageFlags.MachineKeySet)
            : CreateCertificate(certPath, password);

        var sha256 = Convert.ToHexString(SHA256.HashData(certificate.RawData));
        File.WriteAllText(hashPath, sha256);
        logger.LogInformation("HTTPS certificate loaded. SHA-256: {CertificateSha256}", sha256);

        return certificate;
    }

    private static X509Certificate2 CreateCertificate(string certPath, string password)
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

        var now = DateTimeOffset.UtcNow;
        using var certificate = request.CreateSelfSigned(now.AddDays(-1), now.AddYears(5));
        var exportableCertificate = new X509Certificate2(
            certificate.Export(X509ContentType.Pfx, password),
            password,
            X509KeyStorageFlags.Exportable | X509KeyStorageFlags.MachineKeySet);

        File.WriteAllBytes(certPath, exportableCertificate.Export(X509ContentType.Pfx, password));
        return exportableCertificate;
    }

    private static string ResolvePath(string contentRoot, string path)
    {
        return Path.IsPathRooted(path)
            ? path
            : Path.GetFullPath(Path.Combine(contentRoot, path));
    }
}
